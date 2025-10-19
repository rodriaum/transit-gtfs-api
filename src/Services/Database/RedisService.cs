using Microsoft.Extensions.Caching.Distributed;
using Tranzor.Interfaces.Database;
using Tranzor.Utils;

namespace Tranzor.Services.Database
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisService> _logger;
        private readonly TimeSpan _duration;

        public RedisService(IDistributedCache cache, ILogger<RedisService> logger, TimeSpan? duration = null)
        {
            _cache = cache;
            _logger = logger;
            _duration = duration ?? Constant.CacheDuration;
            
            LogRedisConfiguration();
        }

        private void LogRedisConfiguration()
        {
            string? connection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            string? instanceName = Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME");
            
            _logger.LogInformation("[Redis] Cache configured with connection: {Connection}", 
                MaskConnectionString(connection));
            _logger.LogInformation("[Redis] Instance name: {InstanceName}", instanceName?.ToLower());
            _logger.LogInformation("[Redis] Default cache duration: {Duration} minutes", 
                _duration.TotalMinutes);
        }

        private string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "not configured";

            var parts = connectionString.Split(',');
            if (parts.Length > 0)
            {
                var hostPort = parts[0].Split(':');
                return hostPort.Length > 1 ? $"{hostPort[0]}:****" : parts[0];
            }
            
            return "configured";
        }

        public async Task<bool> IsRedisAvailable()
        {
            try
            {
                var testKey = "redis_health_check";
                await _cache.SetStringAsync(testKey, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) });
                var value = await _cache.GetStringAsync(testKey);
                
                if (value == "1")
                {
                    _logger.LogDebug("[Redis] Health check passed");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] Health check failed");
                return false;
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory) where T : class
        {
            if (!await IsRedisAvailable())
            {
                _logger.LogWarning("[Redis] Unavailable. Skipping cache for key: {Key}", key);
                return await factory();
            }

            try
            {
                key = key.ToLower();
                string? data = null;

                try
                {
                    data = await _cache.GetStringAsync(key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Redis] Error accessing (auth/connection issue?). Skipping cache for key: {Key}", key);
                    return await factory();
                }

                if (!string.IsNullOrEmpty(data))
                {
                    _logger.LogDebug("[Redis] Cache hit for key: {Key}", key);
                    return await JsonUtil.StringToObjectAsync<T>(data);
                }

                _logger.LogDebug("[Redis] Cache miss for key: {Key}", key);

                T result = await factory();

                if (result != null)
                {
                    string? json = await JsonUtil.ObjectToStringAsync<T>(result);

                    if (!string.IsNullOrEmpty(json) && !json.Equals("[]"))
                    {
                        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _duration
                        };

                        try
                        {
                            await _cache.SetStringAsync(key, json, options);
                            _logger.LogDebug("[Redis] Item cached with key: {Key}", key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[Redis] Error setting item in cache with key: {Key}", key);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error getting or setting item in cache with key: {Key}", key);
                return await factory();
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (!await IsRedisAvailable())
            {
                _logger.LogWarning("[Redis] Unavailable. Skipping remove for key: {Key}", key);
                return;
            }
            try
            {
                key = key.ToLower();
                await _cache.RemoveAsync(key);
                _logger.LogDebug("[Redis] Removed item from cache with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error removing item from cache with key: {Key}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            _logger.LogWarning("[Redis] {Method} not fully implemented. Prefix: {Prefix}", nameof(RemoveByPrefixAsync), prefix);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (!await IsRedisAvailable())
            {
                _logger.LogWarning("[Redis] Unavailable. Skipping exists check for key: {Key}", key);
                return false;
            }
            try
            {
                key = key.ToLower();
                string? data = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error checking if key exists in cache: {Key}", key);
                return false;
            }
        }
    }
}