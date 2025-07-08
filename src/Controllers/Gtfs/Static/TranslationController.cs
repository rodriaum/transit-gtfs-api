using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/translations")]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _service;
    public TranslationController(ITranslationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Translation>>> GetAll() => await _service.GetAllAsync();

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string directoryPath)
    {
        await _service.ImportDataAsync(directoryPath);
        return Ok();
    }
}
