namespace Tranzor.Services.Gtfs.Realtime;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TransitRealtime;
using Tranzor;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Gtfs.Realtime;
using Tranzor.Interfaces.MQTT;
using Tranzor.Models.Config;

public class GtfsRealtimeCacheService : IGtfsRealtimeCacheService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GtfsRealtimeCacheService> _logger;
    private readonly IGtfsMqttRealtimeService _mqttService;

    private static readonly ConcurrentDictionary<string, (FeedMessage feed, DateTime cachedAt)> _httpCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public GtfsRealtimeCacheService(
        ILogger<GtfsRealtimeCacheService> logger,
        IGtfsMqttRealtimeService mqttService)
    {
        _httpClient = new HttpClient();
        _logger = logger;
        _mqttService = mqttService;
    }

    #region Class Functions

    public async Task<FeedMessage?> GetFeedAsync(GtfsDataRealtime gtfsData, string cacheKey, CancellationToken cancellationToken = default)
    {
        if (gtfsData == null) return null;

        string? url = gtfsData.Url;
        if (string.IsNullOrEmpty(gtfsData.Url)) return null;

        RealtimeFileType? fileType = gtfsData.RealtimeFileType;
        if (fileType == null) return null;

        switch (fileType)
        {
            case RealtimeFileType.HTTP:
                return await GetHttpFeedAsync(url, cacheKey, cancellationToken);

            case RealtimeFileType.WebSocket:
                return await GetWebSocketFeedAsync(gtfsData, cacheKey, cancellationToken);

            default:
                _logger.LogWarning($"Unsupported file type: {fileType}");
                return null;
        }
    }

    /// <summary>
    /// Fetches the alerts.pb GTFS-Realtime feed
    /// </summary>
    public async Task<List<Alert>?> GetAlertsAsync(
        string? agencyId = null,
        string? routeId = null,
        string? tripId = null,
        string? stopId = null,
        ulong? startTimestamp = null,
        ulong? endStartTimestamp = null,
        DirectionType? directionType = null,
        RouteType? routeType = null,
        CancellationToken cancellationToken = default)
    {
        List<FeedMessage> messages = new List<FeedMessage>();

        if (agencyId != null)
        {
            FeedMessage? message = await GetFeedAsync(
                GetGtfsDataRealtimeByAgency(agencyId, RealtimeType.ServiceAlerts),
                $"alerts_{agencyId}",
                cancellationToken
            );
            if (message != null)
            {
                messages.Add(message);
            }
        }
        else
        {
            foreach (GtfsData gtfsData in GtfsDataContext.GtfsDataList)
            {
                FeedMessage? message = await GetFeedAsync(
                    GetGtfsDataRealtimeByAgency(gtfsData.AgencyId, RealtimeType.ServiceAlerts),
                    $"alerts_{gtfsData.AgencyId}",
                    cancellationToken
                );

                if (message != null)
                {
                    messages.Add(message);
                }
            }
        }

        if (messages == null || !messages.Any())
            return null;

        List<Alert> result = new List<Alert>();

        foreach (FeedMessage message in messages)
        {
            result.AddRange(message.Entity
                .Where(entity => entity.Alert != null)
                .Select(entity => entity.Alert)
                .ToList()
            );
        }

        result.RemoveAll(alert =>
        {
            if (alert.ActivePeriod != null && alert.ActivePeriod.Count > 0)
            {
                bool validPeriod = alert.ActivePeriod.Any(period =>
                    (!startTimestamp.HasValue || (period.HasStart && period.Start >= startTimestamp.Value)) &&
                    (!endStartTimestamp.HasValue || (period.HasEnd && period.End >= endStartTimestamp.Value))
                );

                if (!validPeriod)
                    return true;
            }

            if (!string.IsNullOrEmpty(routeId) &&
                !alert.InformedEntity.Any(e => e.RouteId == routeId))
                return true;

            if (!string.IsNullOrEmpty(tripId) &&
                !alert.InformedEntity.Any(e => e.Trip != null && e.Trip.TripId == tripId))
                return true;

            if (!string.IsNullOrEmpty(stopId) &&
                !alert.InformedEntity.Any(e => e.StopId == stopId))
                return true;

            if (directionType.HasValue &&
                !alert.InformedEntity.Any(e => e.Trip != null && e.Trip.DirectionId == (int)directionType.Value))
                return true;

            if (routeType.HasValue &&
                !alert.InformedEntity.Any(e => e.HasRouteType && e.RouteType == (int)routeType.Value))
                return true;

            return false;
        });

        return result;
    }

    /// <summary>
    /// Fetches the vehicles.pb GTFS-Realtime feed
    /// </summary>
    public async Task<List<VehiclePosition>?> GetVehiclePositionsAsync(
        string? agencyId = null,
        string? stopId = null,
        string? tripId = null,
        string? routeId = null,
        string? vehicleId = null,
        string? vehicleLabel = null,
        string? vehicleLicensePlate = null,
        ulong? startTimestamp = null,
        ulong? endStartTimestamp = null,
        DirectionType? directionType = null,
        CancellationToken cancellationToken = default)
    {
        List<FeedMessage> messages = new List<FeedMessage>();

        if (agencyId != null)
        {
            FeedMessage? message = await GetFeedAsync(
                GetGtfsDataRealtimeByAgency(agencyId, RealtimeType.VehiclePositions),
                $"vehicle_positions_{agencyId}",
                cancellationToken
            );
            if (message != null)
            {
                messages.Add(message);
            }
        }
        else
        {
            foreach (GtfsData gtfsData in GtfsDataContext.GtfsDataList)
            {
                FeedMessage? message = await GetFeedAsync(
                    GetGtfsDataRealtimeByAgency(gtfsData.AgencyId, RealtimeType.VehiclePositions),
                    $"vehicle_positions_{gtfsData.AgencyId}",
                    cancellationToken
                );

                if (message != null)
                {
                    messages.Add(message);
                }
            }
        }

        if (messages == null || !messages.Any())
            return null;

        List<VehiclePosition> result = new List<VehiclePosition>();

        foreach (FeedMessage message in messages)
        {
            result.AddRange(message.Entity
                .Where(entity => entity.Vehicle != null)
                .Select(entity => entity.Vehicle)
                .ToList()
            );
        }

        if (result == null || !result.Any())
            return null;

        result.RemoveAll(v =>
        {
            if (!string.IsNullOrEmpty(stopId) &&
                !v.StopId.Equals(stopId, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(tripId) &&
                (v.Trip == null || v.Trip.TripId != tripId))
                return true;

            if (!string.IsNullOrEmpty(routeId) &&
                (v.Trip == null || v.Trip.RouteId != routeId))
                return true;

            if (directionType.HasValue &&
                (v.Trip == null || v.Trip.DirectionId != (int)directionType.Value))
                return true;

            if (!string.IsNullOrEmpty(vehicleId) &&
                (v.Vehicle == null || v.Vehicle.Id != vehicleId))
                return true;

            if (!string.IsNullOrEmpty(vehicleLabel) &&
                (v.Vehicle == null || v.Vehicle.Label != vehicleLabel))
                return true;

            if (!string.IsNullOrEmpty(vehicleLicensePlate) &&
                (v.Vehicle == null || v.Vehicle.LicensePlate != vehicleLicensePlate))
                return true;

            if (startTimestamp.HasValue && v.HasTimestamp && v.Timestamp < startTimestamp.Value)
                return true;

            if (endStartTimestamp.HasValue && v.HasTimestamp && v.Timestamp > endStartTimestamp.Value)
                return true;

            return false;
        });

        return result;
    }

    public async Task<List<TripUpdate>?> GetTripUpdatesAsync(
    string? agencyId = null,
    string? stopId = null,
    string? tripId = null,
    string? routeId = null,
    string? vehicleId = null,
    DirectionType? directionType = null,
    ulong? startTimestamp = null,
    ulong? endStartTimestamp = null,
    CancellationToken cancellationToken = default)
    {
        List<FeedMessage> messages = new List<FeedMessage>();

        if (agencyId != null)
        {
            GtfsDataRealtime? gtfsDataRealtime = GetGtfsDataRealtimeByAgency(agencyId, RealtimeType.TripUpdates);
            if (gtfsDataRealtime == null) return null;

            FeedMessage? message = await GetFeedAsync(
                gtfsDataRealtime,
                $"trip_updates_{agencyId}",
                cancellationToken
            );
            if (message != null)
            {
                messages.Add(message);
            }
        }
        else
        {
            foreach (GtfsData gtfsData in GtfsDataContext.GtfsDataList)
            {
                GtfsDataRealtime? gtfsDataRealtime = GetGtfsDataRealtimeByAgency(gtfsData.AgencyId, RealtimeType.TripUpdates);
                if (gtfsDataRealtime == null) continue;

                FeedMessage? message = await GetFeedAsync(
                    gtfsDataRealtime,
                    $"trip_updates_{gtfsData.AgencyId}",
                    cancellationToken
                );

                if (message != null)
                {
                    messages.Add(message);
                }
            }
        }

        if (messages == null || !messages.Any())
            return null;

        List<TripUpdate> result = new List<TripUpdate>();

        foreach (FeedMessage message in messages)
        {
            result.AddRange(message.Entity
                .Where(entity => entity.TripUpdate != null)
                .Select(entity => entity.TripUpdate)
                .ToList()
            );
        }

        result.RemoveAll(update =>
        {
            if (!string.IsNullOrEmpty(tripId) &&
                (update.Trip == null || update.Trip.TripId != tripId))
                return true;

            if (!string.IsNullOrEmpty(routeId) &&
                (update.Trip == null || update.Trip.RouteId != routeId))
                return true;

            if (directionType.HasValue &&
                (update.Trip == null || update.Trip.DirectionId != (int)directionType.Value))
                return true;

            if (!string.IsNullOrEmpty(vehicleId) &&
                (update.Vehicle == null || update.Vehicle.Id != vehicleId))
                return true;

            if (!string.IsNullOrEmpty(stopId) &&
                !update.StopTimeUpdate.Any(stu => stu.StopId == stopId))
                return true;

            if (startTimestamp.HasValue && update.HasTimestamp && update.Timestamp < startTimestamp.Value)
                return true;

            if (endStartTimestamp.HasValue && update.HasTimestamp && update.Timestamp > endStartTimestamp.Value)
                return true;

            return false;
        });

        return result;
    }

    #endregion
    #region Helper Functions

    private async Task<FeedMessage?> GetHttpFeedAsync(string url, string cacheKey, CancellationToken cancellationToken)
    {
        if (_httpCache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.cachedAt < Constant.CacheDuration)
        {
            return cached.feed;
        }

        SemaphoreSlim lockObj = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken);

        try
        {
            if (_httpCache.TryGetValue(cacheKey, out cached) &&
                DateTime.UtcNow - cached.cachedAt < Constant.CacheDuration)
            {
                return cached.feed;
            }

            _logger.LogDebug($"Fetching HTTP feed from: {url}");

            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            FeedMessage feed = FeedMessage.Parser.ParseFrom(bytes);

            if (feed != null)
            {
                _httpCache[cacheKey] = (feed, DateTime.UtcNow);
                _logger.LogDebug($"HTTP feed cached for {cacheKey}: {feed.Entity.Count} entities");
            }

            return feed;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to fetch HTTP feed from {url}: {ex.Message}");

            return _httpCache.TryGetValue(cacheKey, out cached) ? cached.feed : null;
        }
        finally
        {
            lockObj.Release();
        }
    }

    private async Task<FeedMessage?> GetWebSocketFeedAsync(GtfsDataRealtime gtfsData, string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            RealtimeType realtimeType = DetermineRealtimeType(cacheKey);

            if (!_mqttService.IsConnected(cacheKey))
            {
                await _mqttService.StartConnectionAsync(gtfsData, cacheKey, realtimeType, cancellationToken);
            }

            return await _mqttService.GetRealtimeFeedAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get WebSocket feed for {cacheKey}: {ex.Message}");
            return null;
        }
    }

    private RealtimeType DetermineRealtimeType(string cacheKey)
    {
        if (cacheKey.Contains("vehicle_positions"))
            return RealtimeType.VehiclePositions;

        if (cacheKey.Contains("trip_updates"))
            return RealtimeType.TripUpdates;

        if (cacheKey.Contains("alerts"))
            return RealtimeType.ServiceAlerts;

        return RealtimeType.VehiclePositions;
    }

    public GtfsDataRealtime? GetGtfsDataRealtimeByAgency(string agencyId, RealtimeType type)
    {
        foreach (GtfsData data in GtfsDataContext.GtfsDataList)
        {
            if (data.AgencyId != agencyId.ToLower()) continue;

            var realtimeUrls = data.RealtimeUrls;
            if (realtimeUrls == null) continue;

            if (realtimeUrls.TryGetValue(type, out var value))
                return value;
        }

        return null;
    }

    #endregion
}