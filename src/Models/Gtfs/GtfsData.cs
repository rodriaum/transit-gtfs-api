using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class GtfsData
{
    public string AgencyId { get; set; }
    public string Url { get; set; }

    public List<string> IgnoredFiles { get; set; } = new List<string>();
    public Dictionary<RealtimeType, string>? RealtimeUrls { get; set; }

    public GtfsData(string agencyId, string url, List<string>? ignoredFiles = null, Dictionary<RealtimeType, string>? realtimeUrls = null)
    {
        AgencyId = agencyId.ToLower();
        Url = url;
        IgnoredFiles = ignoredFiles ?? new List<string>();
        RealtimeUrls = realtimeUrls;
    }
}