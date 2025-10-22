using Microsoft.Extensions.Caching.Distributed;
using Tranzor.Interfaces.Database;
using Tranzor.Utils;

namespace Tranzor.Services.Database
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisService> _logger;
        private readonly TimeSpan _defaultDuration;
        private readonly SemaphoreSlim _healthCheckLock = new(1, 1);
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private bool _lastHealthCheckResult;
        private readonly TimeSpan _healthCheckCacheDuration = TimeSpan.FromSeconds(30);

        public RedisService(IDistributedCache cache, ILogger<RedisService> logger, TimeSpan? duration = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultDuration = duration ?? Constant.CacheDuration; }

        private void LogRedisConfiguration()
        {
            string? connection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            string? instanceName = Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME");

            _logger.LogInformation("[Redis] Cache configured with connection: {Connection}",
                MaskConnectionString(connection));
            _logger.LogInformation("[Redis] Instance name: {InstanceName}", instanceName ?? "not set");
            _logger.LogInformation("[Redis] Default cache duration: {Duration} minutes",
                _defaultDuration.TotalMinutes);
        }

        private static string MaskConnectionString(string? connectionString)
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
        
        public async Task RSetupAsync()
        {
            string time = DateTime.UtcNow.ToString("o");
            await SetAsync("api:start-time", time);
            
            LogRedisConfiguration();
        }

        public async Task<bool> IsRedisAvailable()
        {
            // Cache health check result to avoid too many checks
            if (DateTime.UtcNow - _lastHealthCheck < _healthCheckCacheDuration)
            {
                return _lastHealthCheckResult;
            }

            await _healthCheckLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (DateTime.UtcNow - _lastHealthCheck < _healthCheckCacheDuration)
                {
                    return _lastHealthCheckResult;
                }

                var testKey = "redis:health:check";
                var testValue = DateTime.UtcNow.Ticks.ToString();
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                };

                await _cache.SetStringAsync(testKey, testValue, options);
                var value = await _cache.GetStringAsync(testKey);

                _lastHealthCheckResult = value == testValue;
                _lastHealthCheck = DateTime.UtcNow;

                if (_lastHealthCheckResult)
                {
                    _logger.LogDebug("[Redis] Health check passed");
                }
                else
                {
                    _logger.LogWarning("[Redis] Health check failed - value mismatch");
                }

                return _lastHealthCheckResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] Health check failed");
                _lastHealthCheckResult = false;
                _lastHealthCheck = DateTime.UtcNow;
                return false;
            }
            finally
            {
                _healthCheckLock.Release();
            }
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                key = NormalizeKey(key);
                var json = await _cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var result = await JsonUtil.StringToObjectAsync<T>(json);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error getting item from cache with key: {Key}", key);
                return null;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                key = NormalizeKey(key);
                var json = await JsonUtil.ObjectToStringAsync<T>(value);

                if (string.IsNullOrEmpty(json) || json == "[]" || json == "{}")
                {
                    _logger.LogWarning("[Redis] Skipping cache for empty/invalid value with key: {Key}", key);
                    return false;
                }

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _defaultDuration
                };

                await _cache.SetStringAsync(key, json, options);
                _logger.LogDebug("[Redis] Item cached with key: {Key}, expiration: {Expiration} minutes",
                    key, options.AbsoluteExpirationRelativeToNow?.TotalMinutes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error setting item in cache with key: {Key}", key);
                return false;
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                key = NormalizeKey(key);
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
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            // Note: IDistributedCache doesn't support pattern-based deletion
            // This would require direct Redis connection (e.g., using StackExchange.Redis)
            // For now, log a warning
            _logger.LogWarning(
                "[Redis] {Method} requires direct Redis access (not implemented with IDistributedCache). Prefix: {Prefix}",
                nameof(RemoveByPrefixAsync), prefix);

            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                key = NormalizeKey(key);
                var data = await _cache.GetStringAsync(key);
                var exists = !string.IsNullOrEmpty(data);

                _logger.LogDebug("[Redis] Key {Key} exists: {Exists}", key, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Redis] Error checking if key exists in cache: {Key}", key);
                return false;
            }
        }

        private static string NormalizeKey(string key)
        {
            return key.ToLowerInvariant().Trim();
        }
    }
}