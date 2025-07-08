using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/stop_areas")]
public class StopAreaController : ControllerBase
{
    private readonly IStopAreaService _service;
    public StopAreaController(IStopAreaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<StopArea>>> GetAll() => await _service.GetAllAsync();
}
