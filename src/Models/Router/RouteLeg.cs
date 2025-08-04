using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models.Router;

public class RouteLeg
{
    public RouteLegModeType Mode { get; set; } = RouteLegModeType.None;
    public Route? Route { get; set; }
    public Stop? From { get; set; } = null!;
    public Stop? To { get; set; } = null!;
    public DateTime? Departure { get; set; }
    public DateTime? Arrival { get; set; }
    public double? DistanceMeters { get; set; }
    public TimeSpan? Duration { get; set; }
}