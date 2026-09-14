using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TicketFlow.API.Interfaces;

namespace TicketFlow.API.Services;

public class CacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        //took from cache
        var cachedValue = await cache.GetStringAsync(key);

        //if has not cached value 
        if (cachedValue is null)
            return default;

        // Deserialize from Json to object
        return JsonSerializer.Deserialize<T>(cachedValue);
    }


    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Use the provided expiration or the default one
        var cacheDuration = expiration ?? null;

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheDuration
        };

        string json = JsonSerializer.Serialize<T>(value);

        await cache.SetStringAsync(key, json, cacheOptions);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}