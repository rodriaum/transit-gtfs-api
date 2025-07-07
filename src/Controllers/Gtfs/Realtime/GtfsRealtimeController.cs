using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<List<VehiclePosition>>> GetVehiclePositionsByAgencyId(string agencyId)
    {
        List<VehiclePosition>? result = await _gtfsRealtimeService.GetVehiclePositionsAsync(agencyId);

        if (result == null)
            return NotFound();

        return result;
    }

    [HttpGet("alerts/{agencyId}")]
    public async Task<ActionResult<List<VehiclePosition>>> GetAlertsByAgencyId(string agencyId)
    {
        List<VehiclePosition>? result = await _gtfsRealtimeService.GetVehiclePositionsAsync(agencyId);

        if (result == null)
            return NotFound();

        return result;
    }
}