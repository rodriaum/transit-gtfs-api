namespace Tranzor.Models;

public class Agency
{
    public string Id { get; set; }
    public string AgencyId { get; set; }
    public string AgencyName { get; set; }
    public string AgencyUrl { get; set; }
    public string AgencyTimezone { get; set; }
    public string AgencyLang { get; set; }
    public string? AgencyPhone { get; set; }
    public string? AgencyFareUrl { get; set; }
    public string? AgencyEmail { get; set; }
}