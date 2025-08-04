namespace TransitGtfsApi.Models.Router;

public class RouteLeg
{
    public string Mode { get; set; } = null!; // "walking" ou "transit"
    public string? Route { get; set; }
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
    public DateTime? Departure { get; set; }
    public DateTime? Arrival { get; set; }
    public double? DistanceMeters { get; set; }
    public TimeSpan? Duration { get; set; }
}