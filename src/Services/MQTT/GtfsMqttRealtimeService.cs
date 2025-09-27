using MQTTnet;
using System.Collections.Concurrent;
using TransitRealtime;
using Tranzor.Enums;
using Tranzor.Interfaces.MQTT;
using Tranzor.Models.Config;

namespace Tranzor.Services.MQTT;

public class GtfsMqttRealtimeService : IGtfsMqttRealtimeService, IDisposable
{
    private readonly ILogger<GtfsMqttRealtimeService> _logger;

    private readonly ConcurrentDictionary<string, IMqttClient> _mqttClients = new();
    private readonly ConcurrentDictionary<string, (FeedMessage feed, DateTime lastUpdate)> _realtimeCache = new();
    private readonly ConcurrentDictionary<string, RealtimeType> _connectionTypes = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLocks = new();

    public GtfsMqttRealtimeService(ILogger<GtfsMqttRealtimeService> logger)
    {
        _logger = logger;
    }

    public async Task<FeedMessage?> GetRealtimeFeedAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_realtimeCache.TryGetValue(cacheKey, out var cachedData))
        {
            if (DateTime.UtcNow - cachedData.lastUpdate < TimeSpan.FromSeconds(30))
            {
                return cachedData.feed;
            }
        }

        return null;
    }

    public async Task StartConnectionAsync(GtfsDataRealtime gtfsData, string cacheKey, RealtimeType realtimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(gtfsData?.Url))
        {
            _logger.LogWarning($"Invalid GTFS data or URL for cache key: {cacheKey}");
            return;
        }

        if (_mqttClients.ContainsKey(cacheKey) && _mqttClients[cacheKey].IsConnected)
        {
            _logger.LogDebug($"MQTT connection already exists and is active for: {cacheKey}");
            return;
        }

        var lockObj = _connectionLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(cancellationToken);

        try
        {
            if (_mqttClients.ContainsKey(cacheKey) && _mqttClients[cacheKey].IsConnected)
                return;

            await CreateMqttConnectionAsync(gtfsData.Url, cacheKey, realtimeType, cancellationToken);
            _connectionTypes[cacheKey] = realtimeType;
        }
        finally
        {
            lockObj.Release();
        }
    }

    public async Task StopConnectionAsync(string cacheKey)
    {
        if (_mqttClients.TryRemove(cacheKey, out var client))
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync();
                    _logger.LogInformation($"Disconnected MQTT client for: {cacheKey}");
                }
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error disconnecting MQTT client for {cacheKey}: {ex.Message}");
            }
        }

        _realtimeCache.TryRemove(cacheKey, out _);
        _connectionTypes.TryRemove(cacheKey, out _);

        if (_connectionLocks.TryRemove(cacheKey, out var lockObj))
        {
            lockObj.Dispose();
        }
    }

    public bool IsConnected(string cacheKey)
    {
        return _mqttClients.TryGetValue(cacheKey, out var client) && client.IsConnected;
    }

    private async Task CreateMqttConnectionAsync(string websocketUrl, string cacheKey, RealtimeType realtimeType, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Creating MQTT connection for {cacheKey} to {websocketUrl}");

            var factory = new MqttClientFactory();
            var client = factory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer(o =>
                {
                    o.WithUri(websocketUrl);
                })
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithTimeout(TimeSpan.FromSeconds(10))
                .Build();

            // Event handlers
            client.ConnectedAsync += async e =>
            {
                _logger.LogInformation($"Connected to MQTT broker: {websocketUrl} for {cacheKey}");
                await SubscribeToTopicsAsync(client, realtimeType, cacheKey);
            };

            client.ApplicationMessageReceivedAsync += async e =>
            {
                await ProcessMqttMessageAsync(e, cacheKey);
            };

            client.DisconnectedAsync += async e =>
            {
                _logger.LogWarning($"Disconnected from MQTT broker for {cacheKey}. Reason: {e.Reason}");
                await HandleDisconnectionAsync(client, mqttClientOptions, cacheKey, cancellationToken);
            };

            await client.ConnectAsync(mqttClientOptions, cancellationToken);
            _mqttClients[cacheKey] = client;

            _logger.LogInformation($"MQTT client successfully created and connected for: {cacheKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create MQTT connection for {cacheKey}: {ex.Message}");
            throw;
        }
    }

    private async Task SubscribeToTopicsAsync(IMqttClient client, RealtimeType realtimeType, string cacheKey)
    {
        try
        {
            string topic = realtimeType switch
            {
                RealtimeType.VehiclePositions => "/gtfsrt/vp/#",
                RealtimeType.TripUpdates => "/gtfsrt/tu/#",
                RealtimeType.ServiceAlerts => "/gtfsrt/alerts/#",
                _ => "/gtfsrt/#"
            };

            await client.SubscribeAsync(topic);
            _logger.LogInformation($"Subscribed to topic '{topic}' for {cacheKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to subscribe to topics for {cacheKey}: {ex.Message}");
        }
    }

    private async Task ProcessMqttMessageAsync(MqttApplicationMessageReceivedEventArgs e, string cacheKey)
    {
        try
        {
            var payload = e.ApplicationMessage.Payload;
            var feed = FeedMessage.Parser.ParseFrom(payload);

            _realtimeCache[cacheKey] = (feed, DateTime.UtcNow);

            _logger.LogDebug($"Processed MQTT message for {cacheKey}: {feed.Entity.Count} entities, Topic: {e.ApplicationMessage.Topic}");

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogDetailedFeedInfo(feed, cacheKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing MQTT message for {cacheKey}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private void LogDetailedFeedInfo(FeedMessage feed, string cacheKey)
    {
        foreach (var entity in feed.Entity)
        {
            _logger.LogTrace($"[{cacheKey}] Entity ID: {entity.Id}");

            if (entity.TripUpdate != null)
            {
                var trip = entity.TripUpdate;
                _logger.LogTrace($"[{cacheKey}] TripUpdate: TripId={trip.Trip?.TripId}, RouteId={trip.Trip?.RouteId}");
            }

            if (entity.Vehicle != null)
            {
                var vehicle = entity.Vehicle;
                _logger.LogTrace($"[{cacheKey}] Vehicle: Id={vehicle.Vehicle?.Id}, TripId={vehicle.Trip?.TripId}, RouteId={vehicle.Trip?.RouteId}, Pos=({vehicle.Position?.Latitude}, {vehicle.Position?.Longitude})");
            }

            if (entity.Alert != null)
            {
                var alert = entity.Alert;
                _logger.LogTrace($"[{cacheKey}] Alert: {alert.InformedEntity.Count} informed entities");
            }
        }
    }

    private async Task HandleDisconnectionAsync(IMqttClient client, MqttClientOptions options, string cacheKey, CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        const int baseDelaySeconds = 2;

        for (int retry = 1; retry <= maxRetries; retry++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var delay = TimeSpan.FromSeconds(baseDelaySeconds * Math.Pow(2, retry - 1)); // Exponential backoff
                _logger.LogInformation($"Attempting to reconnect {cacheKey} in {delay.TotalSeconds} seconds (attempt {retry}/{maxRetries})");

                await Task.Delay(delay, cancellationToken);

                await client.ConnectAsync(options, cancellationToken);
                _logger.LogInformation($"Successfully reconnected {cacheKey}");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Reconnection attempt {retry} failed for {cacheKey}: {ex.Message}");

                if (retry == maxRetries)
                {
                    _logger.LogError($"Failed to reconnect {cacheKey} after {maxRetries} attempts. Removing from active connections.");
                    await StopConnectionAsync(cacheKey);
                }
            }
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing GtfsMqttRealtimeService...");

        List<Task> disconnectTasks = new List<Task>();

        foreach (var kvp in _mqttClients)
        {
            string cacheKey = kvp.Key;
            IMqttClient client = kvp.Value;

            disconnectTasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(new MqttClientDisconnectOptions()
                        {
                            SessionExpiryInterval = 5
                        });
                    }
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disposing MQTT client for {cacheKey}: {ex.Message}");
                }
            }));
        }

        try
        {
            Task.WaitAll(disconnectTasks.ToArray(), TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during disposal: {ex.Message}");
        }

        _mqttClients.Clear();
        _realtimeCache.Clear();
        _connectionTypes.Clear();

        foreach (var lockObj in _connectionLocks.Values)
        {
            lockObj.Dispose();
        }
        _connectionLocks.Clear();

        _logger.LogInformation("GtfsMqttRealtimeService disposed successfully");
    }
}