using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/stop_areas")]
public class StopAreaController : ControllerBase
{
    private readonly IStopAreaService _service;
    public StopAreaController(IStopAreaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<StopArea>>> GetAll() => await _service.GetAllAsync();
}
