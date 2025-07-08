namespace TransitGtfsApi.Services.Gtfs.Realtime;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TransitGtfsApi;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Gtfs.Realtime;
using TransitRealtime;

public class GtfsRealtimeCacheService : IGtfsRealtimeCacheService
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);
    private readonly string _cacheFolder;

    private readonly ILogger<GtfsRealtimeCacheService> _logger;

    public GtfsRealtimeCacheService(ILogger<GtfsRealtimeCacheService> logger)
    {
        _httpClient = new HttpClient();
        _cacheFolder = Constant.ExtractPathRealtime;
        _logger = logger;

        if (!Directory.Exists(_cacheFolder))
            Directory.CreateDirectory(_cacheFolder);
    }

    /// <summary>
    /// Fetches and parses a GTFS Realtime feed with caching.
    /// If cache is valid (less than 30s), reads from local file.
    /// Otherwise, downloads from URL and updates the cache.
    /// </summary>
    public async Task<FeedMessage?> GetFeedAsync(string url, string cacheFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("URL is null or empty. Returning null.");
            return null;
        }

        string cachePath = Path.Combine(_cacheFolder, cacheFileName);

        // Use cache if it's still valid
        if (File.Exists(cachePath))
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(cachePath);

            if (DateTime.UtcNow - lastWrite < _cacheDuration)
            {
                try
                {
                    await using var cacheStream = File.OpenRead(cachePath);
                    return FeedMessage.Parser.ParseFrom(cacheStream);
                }
                catch
                {
                    // If cache is corrupted, continue and try downloading
                }
            }
        }

        try
        {
            await using Stream responseStream = await _httpClient.GetStreamAsync(url, cancellationToken);

            await using (FileStream fileStream = File.Create(cachePath))
            {
                await responseStream.CopyToAsync(fileStream, cancellationToken);
            }

            await using FileStream finalStream = File.OpenRead(cachePath);
            return FeedMessage.Parser.ParseFrom(finalStream);
        }
        catch (Exception ex)
        {
            // If download fails, attempt to read existing cache
            if (File.Exists(cachePath))
            {
                try
                {
                    await using FileStream fallbackStream = File.OpenRead(cachePath);
                    return FeedMessage.Parser.ParseFrom(fallbackStream);
                }
                catch
                {
                    _logger.LogWarning($"Failed to parse both downloaded and cached feed\n -> {ex.Message}");
                    return null;
                }
            }

            _logger.LogWarning($"Failed to fetch feed and no valid cache available.\n -> {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches the alerts.pb GTFS-Realtime feed
    /// </summary>
    public async Task<List<Alert>?> GetAlertsAsync(
        string agencyId,
        string? routeId = null,
        string? tripId = null,
        string? stopId = null,
        ulong? startTimestamp = null,
        ulong? endStartTimestamp = null,
        DirectionType? directionType = null,
        RouteType? routeType = null,
        CancellationToken cancellationToken = default)
    {
        FeedMessage? message = await GetFeedAsync(GetPathByAgency(agencyId, RealtimeType.ServiceAlerts) ?? "", "alerts.pb", cancellationToken);

        if (message == null)
            return null;

        List<Alert> result = message.Entity
            .Where(entity => entity.Alert != null)
            .Select(entity => entity.Alert)
            .ToList();

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
        string agencyId,
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
        FeedMessage? message = await GetFeedAsync(GetPathByAgency(agencyId, RealtimeType.VehiclePositions) ?? "", "vehicle_positions.pb", cancellationToken);
        if (message == null)
            return null;

        List<VehiclePosition> result = message.Entity
            .Where(entity => entity.Vehicle != null)
            .Select(entity => entity.Vehicle)
            .ToList();

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
    string agencyId,
    string? stopId = null,
    string? tripId = null,
    string? routeId = null,
    string? vehicleId = null,
    DirectionType? directionType = null,
    ulong? startTimestamp = null,
    ulong? endStartTimestamp = null,
    CancellationToken cancellationToken = default)
    {
        FeedMessage? message = await GetFeedAsync(GetPathByAgency(agencyId, RealtimeType.TripUpdates) ?? "", "trip_updates.pb", cancellationToken);
        if (message == null)
            return null;

        List<TripUpdate> result = message.Entity
            .Where(entity => entity.TripUpdate != null)
            .Select(entity => entity.TripUpdate)
            .ToList();

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


    public string? GetPathByAgency(string agencyId, RealtimeType type) =>
        Constant.GtfsDataList
            .Where(data => data.AgencyId == agencyId)
            .Select(data => data.RealtimeUrls?.GetValueOrDefault(type))
            .FirstOrDefault();
}