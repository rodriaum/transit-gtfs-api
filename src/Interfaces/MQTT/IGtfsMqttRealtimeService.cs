using TransitRealtime;
using Tranzor.Enums;
using Tranzor.Models.Config;

namespace Tranzor.Interfaces.MQTT;

public interface IGtfsMqttRealtimeService
{
    Task<FeedMessage?> GetRealtimeFeedAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task StartConnectionAsync(GtfsDataRealtime gtfsData, string cacheKey, RealtimeType realtimeType, CancellationToken cancellationToken = default);
    Task StopConnectionAsync(string cacheKey);
    bool IsConnected(string cacheKey);
    void Dispose();
}