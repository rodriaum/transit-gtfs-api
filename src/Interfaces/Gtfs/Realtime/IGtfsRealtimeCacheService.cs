using TransitRealtime;

namespace TransitGtfsApi.Interfaces.Gtfs.Realtime;


public interface IGtfsRealtimeCacheService
{
    Task<FeedMessage?> GetFeedAsync(string url, string cacheFileName, CancellationToken cancellationToken = default);

    Task<List<Alert>> GetAlertsAsync(string agencyId, CancellationToken cancellationToken = default);

    Task<List<VehiclePosition>> GetVehiclePositionsAsync(string agencyId, CancellationToken cancellationToken = default);

    string? GetPathByAgency(string agencyId, Enums.RealtimeType type);
}