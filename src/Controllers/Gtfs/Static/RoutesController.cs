using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class RoutesController : ControllerBase
{
    private readonly IRoutesService _routesService;

    public RoutesController(IRoutesService routesService)
    {
        _routesService = routesService;
    }

    [HttpGet("routes")]
    public async Task<ActionResult<List<Models.Route>>> GetAll()
    {
        return await _routesService.GetAllAsync();
    }

    [HttpGet("routes/{id}")]
    public async Task<ActionResult<Models.Route>> GetById(string id)
    {
        var route = await _routesService.GetByIdAsync(id);
        if (route == null)
            return NotFound();

        return route;
    }
}