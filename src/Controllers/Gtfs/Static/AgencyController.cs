using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class AgencyController : ControllerBase
{
    private readonly IAgencyService _agencyService;

    public AgencyController(IAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    [HttpGet("agencies")]
    public async Task<ActionResult<List<Agency>>> GetAll()
    {
        return await _agencyService.GetAllAsync();
    }

    [HttpGet("agencies/{id}")]
    public async Task<ActionResult<Agency>> GetById(string id)
    {
        Agency? agency = await _agencyService.GetByIdAsync(id);

        if (agency == null)
            return NotFound();

        return agency;
    }
}