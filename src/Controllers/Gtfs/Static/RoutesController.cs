using Tranzor.Models;
using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/tranzor")]
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