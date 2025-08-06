using Microsoft.AspNetCore.Mvc;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;

namespace Tranzor.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/feed_info")]
public class FeedInfoController : ControllerBase
{
    private readonly IFeedInfoService _service;
    public FeedInfoController(IFeedInfoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FeedInfo>>> GetAll() => await _service.GetAllAsync();
}
