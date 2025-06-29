using TransitGtfsApi.Models;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace TransitGtfsApi.Controller.Metro;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class FareAttributesController : ControllerBase
{
    private readonly IFareAttributesService _fareAttributesService;

    public FareAttributesController(IFareAttributesService fareAttributesService)
    {
        _fareAttributesService = fareAttributesService;
    }

    [HttpGet("fare-attributes")]
    public async Task<ActionResult<List<FareAttribute>>> GetAll()
    {
        return await _fareAttributesService.GetAllAsync();
    }

    [HttpGet("fare-attributes/{id}")]
    public async Task<ActionResult<FareAttribute>> GetById(string id)
    {
        FareAttribute? fareAttribute = await _fareAttributesService.GetByIdAsync(id);

        if (fareAttribute == null)
            return NotFound();

        return fareAttribute;
    }
}