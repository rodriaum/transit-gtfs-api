namespace Tranzor.Models;

public class FeedInfo
{
    public string Id { get; set; }
    public string FeedPublisherName { get; set; }
    public string FeedPublisherUrl { get; set; }
    public string FeedLang { get; set; }
    public string? FeedStartDate { get; set; }
    public string? FeedEndDate { get; set; }
    public string? FeedVersion { get; set; }
    public string? FeedContactEmail { get; set; }
    public string? FeedContactUrl { get; set; }
}
