using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.External;
using Tranzor.Models;
using Tranzor.Models.External;

namespace Tranzor.Controllers.Gtfs.External;

[ApiController]
[Route("api/v1/tranzor")]
public class CityController : ControllerBase
{
    private readonly ICityService _cityService;

    public CityController(ICityService cityService)
    {
        _cityService = cityService;
    }

    [HttpGet("cities")]
    public async Task<ActionResult<List<City>>> GetAll()
    {
        return await _cityService.GetAllAsync();
    }

    [HttpGet("cities/{id}")]
    public async Task<ActionResult<City>> GetById(string id)
    {
        City? city = await _cityService.GetByIdAsync(id);

        if (city == null)
            return NotFound();

        return city;
    }
}