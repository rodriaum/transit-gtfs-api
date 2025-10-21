namespace Tranzor.Interfaces.Database;

public interface IRedisService
{
    /// <summary>
    /// Gets a value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Sets a value in cache
    /// </summary>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    
    /// <summary>
    /// Removes a single key from cache
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Removes all keys matching the prefix
    /// </summary>
    Task RemoveByPrefixAsync(string prefix);
    
    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Initializes the Redis connection
    /// </summary>
    Task RSetupAsync();
    
    /// <summary>
    /// Checks if Redis is available and healthy
    /// </summary>
    Task<bool> IsRedisAvailable();
}
