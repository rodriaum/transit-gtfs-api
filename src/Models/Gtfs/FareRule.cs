namespace TransitGtfsApi.Models;

public class FareRule
{
    public string Id { get; set; }
    public string FareId { get; set; }
    public string? RouteId { get; set; }
    public string? OriginId { get; set; }
    public string? DestinationId { get; set; }
    public string? ContainsId { get; set; }
}