namespace TransitGtfsApi.Models;

public class FareLegRule
{
    public string Id { get; set; }
    public string FareLegRuleId { get; set; }
    public string? FareProductId { get; set; }
    public string? LegGroupId { get; set; }
    public string? NetworkId { get; set; }
}
