using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Gtfs.Realtime;
using TransitGtfsApi.Models;
using TransitRealtime;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class GtfsRealtimeController : ControllerBase
{
    private readonly IGtfsRealtimeCacheService _gtfsRealtimeService;

    public GtfsRealtimeController(IGtfsRealtimeCacheService gtfsRealtimeService)
    {
        _gtfsRealtimeService = gtfsRealtimeService;
    }

    [HttpGet("vehicle-positions/{agencyId}")]
    public async Task<ActionResult<List<VehiclePosition>>> GetVehiclePositionsByAgencyId(
        string agencyId,
        [FromQuery] string? stopId = null,
        [FromQuery] string? tripId = null,
        [FromQuery] string? routeId = null,
        [FromQuery] string? vehicleId = null,
        [FromQuery] string? vehicleLabel = null,
        [FromQuery] string? vehicleLicensePlate = null,
        [FromQuery] ulong? startTimestamp = null,
        [FromQuery] ulong? endStartTimestamp = null,
        [FromQuery] DirectionType? directionType = null)
    {
        List<VehiclePosition>? result = await _gtfsRealtimeService.GetVehiclePositionsAsync(
            agencyId, stopId, tripId, routeId, vehicleId, vehicleLabel, vehicleLicensePlate, startTimestamp, endStartTimestamp, directionType);

        if (result == null)
            return NotFound();

        return result;
    }

    [HttpGet("alerts/{agencyId}")]
    public async Task<ActionResult<List<Alert>>> GetAlertsByAgencyId(
        string agencyId,
        [FromQuery] string? routeId = null,
        [FromQuery] string? tripId = null,
        [FromQuery] string? stopId = null,
        [FromQuery] ulong? startTimestamp = null,
        [FromQuery] ulong? endStartTimestamp = null,
        [FromQuery] DirectionType? directionType = null,
        [FromQuery] RouteType? routeType = null)
    {
        List<Alert>? result = await _gtfsRealtimeService.GetAlertsAsync(
            agencyId, routeId, tripId, stopId, startTimestamp, endStartTimestamp, directionType, routeType);

        if (result == null)
            return NotFound();

        return result;
    }

    [HttpGet("trip-updates/{agencyId}")]
    public async Task<ActionResult<List<TripUpdate>>> GetTripUpdatesByAgencyId(
        string agencyId,
        [FromQuery] string? stopId = null,
        [FromQuery] string? tripId = null,
        [FromQuery] string? routeId = null,
        [FromQuery] string? vehicleId = null,
        [FromQuery] DirectionType? directionType = null,
        [FromQuery] ulong? startTimestamp = null,
        [FromQuery] ulong? endStartTimestamp = null)
    {
        List<TripUpdate>? result = await _gtfsRealtimeService.GetTripUpdatesAsync(
            agencyId, stopId, tripId, routeId, vehicleId, directionType, startTimestamp, endStartTimestamp);

        if (result == null)
            return NotFound();

        return result;
    }
}