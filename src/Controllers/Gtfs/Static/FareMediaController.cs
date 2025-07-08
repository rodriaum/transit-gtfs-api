using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/fare_media")]
public class FareMediaController : ControllerBase
{
    private readonly IFareMediaService _service;
    public FareMediaController(IFareMediaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FareMedia>>> GetAll() => await _service.GetAllAsync();

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string directoryPath)
    {
        await _service.ImportDataAsync(directoryPath);
        return Ok();
    }
}
