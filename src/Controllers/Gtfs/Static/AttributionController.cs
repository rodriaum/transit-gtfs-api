using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/attributions")]
public class AttributionController : ControllerBase
{
    private readonly IAttributionService _service;
    public AttributionController(IAttributionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Attribution>>> GetAll() => await _service.GetAllAsync();
}
