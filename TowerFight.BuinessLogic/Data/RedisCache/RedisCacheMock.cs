using Microsoft.Extensions.Caching.Memory;

namespace TowerFight.BusinessLogic.Data.RedisCache;

public class RedisCacheMock(IMemoryCache _memoryCache) : IRedisCache
{
    public Task<bool> AddAsync<T>(string cacheSet, string key, T value)
    {
        _memoryCache.Set(key, value);
        return Task.FromResult(true);
    }

    public Task<T?> GetAsync<T>(string cacheSet, string key) where T : class
    {
        return Task.FromResult(_memoryCache.Get<T>(key));
    }

    public Task RemoveAsync(string cacheSet, string key)
    {
        _memoryCache.Remove(key);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string cacheSet, string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }

    public Task<bool> AddStringAsync<T>(string cacheSet, string key, T value, TimeSpan expiration)
    {
        _memoryCache.Set(key, value);
        return Task.FromResult(true);
    }

    public Task<T?> GetStringAsync<T>(string cacheSet, string key) where T : class
    {
        return Task.FromResult(_memoryCache.Get<T>(key));
    }

    public Task RemoveStringAsync(string cacheSet, string key)
    {
        _memoryCache.Remove(key);
        return Task.FromResult(true);
    }

    public Task<bool> StringExistsAsync(string cacheSet, string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }
}