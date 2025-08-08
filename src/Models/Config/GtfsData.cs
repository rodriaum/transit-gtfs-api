using System.Text.Json.Serialization;
using Tranzor.Enums;

namespace Tranzor.Models.Config;

public class GtfsData
{
    [JsonPropertyName("agency_id")]
    public string AgencyId { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("ignored_files")]
    public List<string> IgnoredFiles { get; set; } = new List<string>();

    [JsonPropertyName("realtime_urls")]
    public Dictionary<RealtimeType, GtfsDataRealtime>? RealtimeUrls { get; set; }

    public GtfsData(string agencyId, string url, List<string>? ignoredFiles = null, Dictionary<RealtimeType, GtfsDataRealtime>? realtimeUrls = null)
    {
        AgencyId = agencyId.ToLower();
        Url = url;
        IgnoredFiles = ignoredFiles ?? new List<string>();
        RealtimeUrls = realtimeUrls;
    }
}