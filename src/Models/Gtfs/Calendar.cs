using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class Calendar
{
    public string Id { get; set; }
    public string ServiceId { get; set; }
    public StatusType Monday { get; set; }
    public StatusType Tuesday { get; set; }
    public StatusType Wednesday { get; set; }
    public StatusType Thursday { get; set; }
    public StatusType Friday { get; set; }
    public StatusType Saturday { get; set; }
    public StatusType Sunday { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}