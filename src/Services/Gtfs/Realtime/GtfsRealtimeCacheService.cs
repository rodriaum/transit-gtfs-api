namespace TransitGtfsApi.Services.Gtfs.Realtime;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TransitGtfsApi;
using TransitGtfsApi.Enums;
using TransitRealtime;

public class GtfsRealtimeCacheService
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
    public async Task<List<Stop>?> GetAlertsAsync(string agencyId, CancellationToken cancellationToken = default)
    {
        FeedMessage? message = await GetFeedAsync(GetPathByAgency(agencyId, RealtimeType.Alerts) ?? "", "alerts.pb", cancellationToken);

        if (message == null)
            return null;

        return message.Entity
            .Where(entity => entity.Stop != null)
            .Select(entity => entity.Stop)
            .ToList();
    }

    /// <summary>
    /// Fetches the vehicles.pb GTFS-Realtime feed
    /// </summary>
    public async Task<List<VehiclePosition>?> GetVehiclePositionsAsync(string agencyId, CancellationToken cancellationToken = default)
    {
        FeedMessage? message = await GetFeedAsync(GetPathByAgency(agencyId, RealtimeType.Vehicles) ?? "", "vehicle_positions.pb", cancellationToken);

        if (message == null)
            return null;

        return message.Entity
            .Where(entity => entity.Vehicle != null)
            .Select(entity => entity.Vehicle)
            .ToList();
    }


    public string? GetPathByAgency(string agencyId, RealtimeType type) =>
        Constant.GtfsDataList
            .Where(data => data.AgencyId == agencyId)
            .Select(data => data.RealtimeUrls?.GetValueOrDefault(type))
            .FirstOrDefault();
}