using TransitRealtime;
using Tranzor.Enums;
using Tranzor.Models.Config;

namespace Tranzor.Interfaces.Gtfs.Realtime;


public interface IGtfsRealtimeCacheService
{
    Task<FeedMessage?> GetFeedAsync(GtfsDataRealtime gtfsDataRealtime, string cacheFileName, CancellationToken cancellationToken = default);
    GtfsDataRealtime? GetGtfsDataRealtimeByAgency(string agencyId, RealtimeType type);

    Task<List<Alert>?> GetAlertsAsync(
        string? agencyId = null,
        string? routeId = null,
        string? tripId = null,
        string? stopId = null,
        ulong? startTimestamp = null,
        ulong? endStartTimestamp = null,
        DirectionType? directionType = null,
        RouteType? routeType = null,
        CancellationToken cancellationToken = default);

    Task<List<VehiclePosition>?> GetVehiclePositionsAsync(
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
        CancellationToken cancellationToken = default);

    Task<List<TripUpdate>?> GetTripUpdatesAsync(
        string? agencyId = null,
        string? stopId = null,
        string? tripId = null,
        string? routeId = null,
        string? vehicleId = null,
        DirectionType? directionType = null,
        ulong? startTimestamp = null,
        ulong? endStartTimestamp = null,
        CancellationToken cancellationToken = default);
}