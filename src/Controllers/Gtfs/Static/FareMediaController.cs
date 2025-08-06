using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/fare_media")]
public class FareMediaController : ControllerBase
{
    private readonly IFareMediaService _service;
    public FareMediaController(IFareMediaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FareMedia>>> GetAll() => await _service.GetAllAsync();
}
