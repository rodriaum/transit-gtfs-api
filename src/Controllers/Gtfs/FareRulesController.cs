using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class FareRulesController : ControllerBase
{
    private readonly IFareRulesService _fareRulesService;

    public FareRulesController(IFareRulesService fareRulesService)
    {
        _fareRulesService = fareRulesService;
    }

    [HttpGet("fare-rules")]
    public async Task<ActionResult<List<FareRule>>> GetAll()
    {
        return await _fareRulesService.GetAllAsync();
    }

    [HttpGet("fare-rules/fare/{fareId}")]
    public async Task<ActionResult<List<FareRule>>> GetByFareId(string fareId)
    {
        List<FareRule>? fareRule = await _fareRulesService.GetByFareIdAsync(fareId);

        if (fareRule == null)
            return NotFound();

        return fareRule;
    }
}