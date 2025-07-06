using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class GtfsData
{
    public string AgencyKey { get; set; }
    public string Url { get; set; }
    public List<string> IgnoredFiles { get; set; } = new List<string>();
    public Dictionary<RealtimeType, string>? RealtimeUrls { get; set; }

    public GtfsData(string agencyKey, string url, List<string>? ignoredFiles = null, Dictionary<RealtimeType, string>? realtimeUrls = null)
    {
        AgencyKey = agencyKey.ToLower();
        Url = url;
        IgnoredFiles = ignoredFiles ?? new List<string>();
        RealtimeUrls = realtimeUrls;
    }
}