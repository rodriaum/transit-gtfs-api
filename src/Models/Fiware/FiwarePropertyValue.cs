using System.Text.Json.Serialization;

namespace Tranzor.Models.Fiware;

public class FiwarePropertyValue<T>
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public T Value { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}