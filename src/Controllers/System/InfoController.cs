using TransitGtfsApi.Models;
using TransitGtfsApi;
using Microsoft.AspNetCore.Mvc;

namespace TransitGtfsApi.Controllers.System;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class InfoController : ControllerBase
{
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(Constant.Version);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
			message = "ok",
            healthy = true,
            timestamp = DateTime.UtcNow
        });
    }
}