using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/fare_leg_rules")]
public class FareLegRuleController : ControllerBase
{
    private readonly IFareLegRuleService _service;
    public FareLegRuleController(IFareLegRuleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FareLegRule>>> GetAll() => await _service.GetAllAsync();
}
