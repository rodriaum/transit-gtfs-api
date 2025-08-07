using System.Text.Json.Serialization;

namespace Tranzor.Models.Fiware;

public class FiwareLocationValue
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}