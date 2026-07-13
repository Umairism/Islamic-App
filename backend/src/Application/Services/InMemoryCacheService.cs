using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;

namespace IslamicApp.Application.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (object Value, DateTime Expiration)> _cache = new();

    public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (item.Expiration > DateTime.UtcNow)
            {
                return Task.FromResult((T)item.Value);
            }
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var exp = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(5));
        _cache[key] = (value, exp);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
