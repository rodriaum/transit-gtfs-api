using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.Interfaces.Gtfs.Static;

namespace TransitGtfsApi.Controllers.Gtfs.Static;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class CalendarDatesController : ControllerBase
{
    private readonly ICalendarDatesService _calendarDatesService;

    public CalendarDatesController(ICalendarDatesService calendarDatesService)
    {
        _calendarDatesService = calendarDatesService;
    }

    [HttpGet("calendar-dates")]
    public async Task<ActionResult<List<CalendarDate>>> GetAll()
    {
        return await _calendarDatesService.GetAllAsync();
    }

    [HttpGet("calendar-dates/service/{serviceId}")]
    public async Task<ActionResult<List<CalendarDate>>> GetByServiceId(string serviceId)
    {
        List<CalendarDate>? calendarDates =  await _calendarDatesService.GetByServiceIdAsync(serviceId);

        if (calendarDates == null)
            return NotFound();

        return calendarDates;
    }
}