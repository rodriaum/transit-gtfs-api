using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class StopsController : ControllerBase
{
    private readonly IStopsService _stopsService;

    public StopsController(IStopsService stopsService)
    {
        _stopsService = stopsService;
    }

    [HttpGet("stops")]
    public async Task<ActionResult<List<Stop>>> GetAll()
    {
        return await _stopsService.GetAllAsync();
    }

    [HttpGet("stops/{id}")]
    public async Task<ActionResult<Stop>> GetById(string id)
    {
        var stop = await _stopsService.GetByIdAsync(id);
        if (stop == null)
            return NotFound();

        return stop;
    }
}