using System.Text.Json.Serialization;

namespace Tranzor.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RealtimeFileType
{
    ProtocolBuffer,
    FiwareJson
}