using TransitGtfsApi.Models;
using TransitGtfsApi.Filters;
using TransitGtfsApi.Interfaces.Gtfs;
using Microsoft.AspNetCore.Mvc;

namespace TransitGtfsApi.Controllers.System;

[ApiController]
[Route("api/v1/transit/gtfs")]
[ServiceFilter(typeof(TokenAuthFilter))]
public class DataController : ControllerBase
{
    private readonly IGtfsDataService _gtfsDataService;
    private readonly ILogger<DataController> _logger;

    public DataController(IGtfsDataService gtfsDataService, ILogger<DataController> logger)
    {
        _gtfsDataService = gtfsDataService;
        _logger = logger;
    }

    [HttpPost("reload-data")]
    public async Task<IActionResult> ReloadData()
    {
        try
        {
            _logger.LogInformation("Starting manual data loading...");

            await _gtfsDataService.LoadDataFromFilesAsync();

            return Ok(new { message = "Data loaded successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading data!");
            return StatusCode(500, new { message = "Error loading data.", error = ex.Message });
        }
    }
}