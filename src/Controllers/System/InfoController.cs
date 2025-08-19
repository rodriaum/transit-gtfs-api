using Tranzor.Models;
using Tranzor;
using Microsoft.AspNetCore.Mvc;

namespace Tranzor.Controllers.System;

[ApiController]
[Route("api/v1/tranzor")]
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