using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DBH.Shared.Infrastructure.Caching;

/// <summary>
/// In-Memory Cache Service - Fallback khi không có Redis
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                var value = JsonSerializer.Deserialize<T>(entry.Value, _jsonOptions);
                return Task.FromResult(value);
            }
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
        var entry = new CacheEntry
        {
            Value = jsonValue,
            ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(30))
        };
        _cache[key] = entry;
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null) return cached;

        var value = await factory(cancellationToken);
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + pattern.Replace("*", ".*") + "$");

        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow) return Task.FromResult(true);
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(false);
    }

    public Task RefreshAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            entry.ExpiresAt = DateTime.UtcNow.Add(expiration);
        }
        return Task.CompletedTask;
    }

    public Task HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:hash";
        var hash = _cache.GetOrAdd(hashKey, _ => new CacheEntry { Value = "{}", ExpiresAt = DateTime.MaxValue });

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(hash.Value) ?? new();
        dict[field] = value;
        hash.Value = JsonSerializer.Serialize(dict);

        return Task.CompletedTask;
    }

    public Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:hash";
        if (_cache.TryGetValue(hashKey, out var entry))
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.Value);
            if (dict?.TryGetValue(field, out var value) == true)
            {
                return Task.FromResult<string?>(value);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashKey = $"{key}:hash";
        if (_cache.TryGetValue(hashKey, out var entry))
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.Value);
            return Task.FromResult(dict ?? new Dictionary<string, string>());
        }
        return Task.FromResult(new Dictionary<string, string>());
    }

    public Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        var entry = _cache.GetOrAdd(key, _ => new CacheEntry { Value = "0", ExpiresAt = DateTime.MaxValue });
        var current = long.Parse(entry.Value);
        current += value;
        entry.Value = current.ToString();
        return Task.FromResult(current);
    }

    public Task PublishAsync(string channel, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("In-memory publish to {Channel}: {Message}", channel, message);
        return Task.CompletedTask;
    }

    private class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
