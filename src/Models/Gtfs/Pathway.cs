namespace TransitGtfsApi.Models;

public class Pathway
{
    public string Id { get; set; }
    public string FromStopId { get; set; }
    public string ToStopId { get; set; }
    public int PathwayMode { get; set; }
    public bool IsBidirectional { get; set; }
    public double? Length { get; set; }
    public int? TraversalTime { get; set; }
    public int? StairCount { get; set; }
    public double? MaxSlope { get; set; }
    public double? MinWidth { get; set; }
    public string? SignpostedAs { get; set; }
    public string? ReversedSignpostedAs { get; set; }
}
