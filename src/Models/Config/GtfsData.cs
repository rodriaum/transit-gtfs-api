using System.Text.Json.Serialization;
using Tranzor.Enums;

namespace Tranzor.Models.Config;

public class GtfsData
{
    [JsonPropertyName("agency_id")]
    public string AgencyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("gtfs_url")]
    public string GtfsUrl { get; set; }

    [JsonPropertyName("gtfs_expire_at")]
    public DateOnly? GtfsExpireAt { get; set; }

    [JsonPropertyName("ignored_files")]
    public List<string> IgnoredFiles { get; set; } = new List<string>();

    [JsonPropertyName("realtime_urls")]
    public Dictionary<RealtimeType, GtfsDataRealtime>? RealtimeUrls { get; set; }

    public GtfsData(string agencyId, string name, string gtfsUrl, DateOnly gtfsExpireAt, List<string>? ignoredFiles = null, Dictionary<RealtimeType, GtfsDataRealtime>? realtimeUrls = null)
    {
        AgencyId = agencyId.ToLower();
        Name = name;
        GtfsUrl = gtfsUrl;
        GtfsExpireAt = gtfsExpireAt;
        IgnoredFiles = ignoredFiles ?? new List<string>();
        RealtimeUrls = realtimeUrls;
    }
}