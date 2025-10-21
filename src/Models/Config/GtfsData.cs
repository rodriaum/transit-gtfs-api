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
    public string? GtfsExpireAt { get; set; }

    [JsonPropertyName("ignored_files")]
    public List<string> IgnoredFiles { get; set; } = new();

    [JsonPropertyName("realtime_urls")]
    public Dictionary<RealtimeType, GtfsDataRealtime>? RealtimeUrls { get; set; }

    public DateOnly? GetExpireAtDateOnly()
    {
        return string.IsNullOrEmpty(this.GtfsExpireAt)
            ? null
            : DateOnly.Parse(this.GtfsExpireAt);
    }
}