using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class TripsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public TripsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    [HttpGet("trips")]
    public async Task<ActionResult<List<Trip>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        return await _tripsService.GetAllAsync(page, pageSize);
    }

    [HttpGet("trips/{id}")]
    public async Task<ActionResult<Trip>> GetById(string id)
    {
        Trip? trip = await _tripsService.GetByIdAsync(id);

        if (trip == null)
            return NotFound();

        return trip;
    }

    [HttpGet("trips/route/{routeId}")]
    public async Task<ActionResult<List<Trip>?>> GetByRouteId(string routeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        List<Trip>? trips = await _tripsService.GetByRouteIdAsync(routeId, page, pageSize);

        if (trips == null)
            return NotFound();

        return trips;
    }

    [HttpPost("trips/batch")]
    public async Task<ActionResult<List<Trip>>> GetTripsBatch([FromBody] List<string> tripIds)
    {
        if (tripIds == null || !tripIds.Any())
            return BadRequest("Trip ID list cannot be empty");

        List<Trip>? trips = await _tripsService.GetTripsBatchAsync(tripIds);

        if (trips == null)
            return NotFound();

        return trips;
    }
}