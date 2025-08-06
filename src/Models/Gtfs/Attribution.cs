namespace Tranzor.Models;

public class Attribution
{
    public string Id { get; set; }
    public string? AgencyId { get; set; }
    public string? RouteId { get; set; }
    public string? TripId { get; set; }
    public string OrganizationName { get; set; }
    public bool IsProducer { get; set; }
    public bool IsOperator { get; set; }
    public bool IsAuthority { get; set; }
    public string? AttributionUrl { get; set; }
    public string? AttributionEmail { get; set; }
    public string? AttributionPhone { get; set; }
}
