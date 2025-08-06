using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class Frequency
{
    public string Id { get; set; }
    public string TripId { get; set; }
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public int HeadwaySecs { get; set; }
    public ExactTimeType? ExactTimes { get; set; }
}
