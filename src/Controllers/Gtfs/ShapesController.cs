using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class ShapesController : ControllerBase
{
    private readonly IShapesService _shapesService;

    public ShapesController(IShapesService shapesService)
    {
        _shapesService = shapesService;
    }

    [HttpGet("shapes")]
    public async Task<ActionResult<List<Shape>>> GetAll()
    {
        return await _shapesService.GetAllAsync();
    }

    [HttpGet("shapes/{shapeId}")]
    public async Task<ActionResult<List<Shape>>> GetByShapeId(string shapeId)
    {
        return await _shapesService.GetByShapeIdAsync(shapeId);
    }
}