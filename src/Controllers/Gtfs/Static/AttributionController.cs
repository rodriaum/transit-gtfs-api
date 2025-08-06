using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/attributions")]
public class AttributionController : ControllerBase
{
    private readonly IAttributionService _service;
    public AttributionController(IAttributionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Attribution>>> GetAll() => await _service.GetAllAsync();
}
