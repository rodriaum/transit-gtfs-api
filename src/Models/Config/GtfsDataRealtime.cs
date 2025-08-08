using System.Text.Json.Serialization;
using Tranzor.Enums;

namespace Tranzor.Models.Config;

public class GtfsDataRealtime
{
    [JsonPropertyName("realtime_file_type")]
    public RealtimeFileType? RealtimeFileType { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}