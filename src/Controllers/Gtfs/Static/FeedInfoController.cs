using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/gtfs/feed_info")]
public class FeedInfoController : ControllerBase
{
    private readonly IFeedInfoService _service;
    public FeedInfoController(IFeedInfoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<FeedInfo>>> GetAll() => await _service.GetAllAsync();
}
