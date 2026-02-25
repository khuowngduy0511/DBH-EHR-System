using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DBH.Shared.Infrastructure.Caching;

/// <summary>
/// Redis Cache Service implementation
/// </summary>
public class RedisCacheService : ICacheService, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IOptions<RedisCacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var configOptions = ConfigurationOptions.Parse(_options.ConnectionString);
        configOptions.ConnectTimeout = _options.ConnectTimeoutMs;
        configOptions.SyncTimeout = _options.SyncTimeoutMs;
        configOptions.AllowAdmin = _options.AllowAdmin;
        configOptions.Ssl = _options.UseSsl;
        configOptions.DefaultDatabase = _options.Database;
        configOptions.AbortOnConnectFail = false;

        _redis = ConnectionMultiplexer.Connect(configOptions);
        _database = _redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _logger.LogInformation("Redis cache connected to {Endpoint}", _options.ConnectionString);
    }

    private string GetKey(string key) => $"{_options.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            var value = await _database.StringGetAsync(fullKey);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            var exp = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            await _database.StringSetAsync(fullKey, jsonValue, exp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key {Key}", key);
        }
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            await _database.KeyDeleteAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPattern = GetKey(pattern);
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: fullPattern).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogInformation("Removed {Count} keys matching pattern {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            await _database.KeyExpireAsync(fullKey, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key {Key}", key);
        }
    }

    public async Task HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            await _database.HashSetAsync(fullKey, field, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Key}:{Field}", key, field);
        }
    }

    public async Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            var value = await _database.HashGetAsync(fullKey, field);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Key}:{Field}", key, field);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            var entries = await _database.HashGetAllAsync(fullKey);
            return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields {Key}", key);
            return new Dictionary<string, string>();
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetKey(key);
            return await _database.StringIncrementAsync(fullKey, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing cache key {Key}", key);
            return 0;
        }
    }

    public async Task PublishAsync(string channel, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to channel {Channel}", channel);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
