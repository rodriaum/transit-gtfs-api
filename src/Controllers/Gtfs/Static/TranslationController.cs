using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/translations")]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _service;
    public TranslationController(ITranslationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AgencyTranslation>>> GetAll() => await _service.GetAllAsync();
}
