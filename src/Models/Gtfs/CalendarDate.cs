using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class CalendarDate
{
    public string Id { get; set; }
    public string ServiceId { get; set; }
    public string Date { get; set; }
    public ExceptionType ExceptionType { get; set; }
}