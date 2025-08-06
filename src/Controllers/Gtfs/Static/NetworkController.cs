using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/networks")]
public class NetworkController : ControllerBase
{
    private readonly INetworkService _service;
    public NetworkController(INetworkService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Network>>> GetAll() => await _service.GetAllAsync();
}
