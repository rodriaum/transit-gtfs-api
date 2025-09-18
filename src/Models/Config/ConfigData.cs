using System.Text.Json.Serialization;
using Tranzor.Enums;

namespace Tranzor.Models.Config;

public class ConfigData
{
    [JsonPropertyName("download_data")]
    public DownloadDataType DownloadData { get; set; } = DownloadDataType.None;

    [JsonPropertyName("ignore_exceeded_trips")]
    public bool IgnoreExceededTrips { get; set; }

    public ConfigData(DownloadDataType downloadData, bool ignoreExceededTrips)
    {
        this.DownloadData = downloadData;
        this.IgnoreExceededTrips = ignoreExceededTrips;
    }
}