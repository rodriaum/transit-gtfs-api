using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("calendars")]
    public async Task<ActionResult<List<Calendar>>> GetAll()
    {
        return await _calendarService.GetAllAsync();
    }

    [HttpGet("calendars/{id}")]
    public async Task<ActionResult<Calendar>> GetById(string id)
    {
        Calendar? calendar = await _calendarService.GetByIdAsync(id);

        if (calendar == null)
            return NotFound();

        return calendar;
    }
}