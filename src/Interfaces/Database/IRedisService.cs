namespace Tranzor.Interfaces.Database;

public interface IRedisService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> dataFactory) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<bool> ExistsAsync(string key);
    Task<bool> IsRedisAvailable();
}
