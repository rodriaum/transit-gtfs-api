using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/tranzor")]
public class StopsController : ControllerBase
{
    private readonly IStopsService _stopsService;

    public StopsController(IStopsService stopsService)
    {
        _stopsService = stopsService;
    }

    [HttpGet("stops")]
    public async Task<ActionResult<List<Stop>>> GetAll(
        [FromQuery] string cityId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 100)
    {
        return await _stopsService.GetAllAsync(cityId, page, pageSize);
    }

    [HttpGet("stops/{id}")]
    public async Task<ActionResult<Stop>> GetById(string id)
    {
        Stop? stop = await _stopsService.GetByIdAsync(id);

        if (stop == null)
            return NotFound();

        return stop;
    }

    [HttpGet("nearby-stops")]
    public async Task<ActionResult<List<Stop>>> GetNearbyStops(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] string cityId,
        [FromQuery] int limit = 1)
    {
        List<Stop> nearestStops = await _stopsService.GetNearestStopAsync(latitude, longitude, cityId, limit);

        if (nearestStops == null || !nearestStops.Any())
            return NotFound();

        foreach (Stop stopItem in nearestStops)
        {
            stopItem.CalcDistAndWalking(latitude, longitude);
        }

        return Ok(nearestStops);
    }
}