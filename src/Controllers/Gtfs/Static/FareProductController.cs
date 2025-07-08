using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/fare_products")]
public class FareProductController : ControllerBase
{
    private readonly IFareProductService _service;
    public FareProductController(IFareProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FareProduct>>> GetAll() => await _service.GetAllAsync();
}
