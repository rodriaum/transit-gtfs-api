using System.Text.Json.Serialization;

namespace Tranzor.Models.Config;

public class ConfigData
{
    [JsonPropertyName("download_data")]
    public bool DownloadData { get; set; }

    public ConfigData(bool downloadData)
    {
        this.DownloadData = downloadData;
    }
}