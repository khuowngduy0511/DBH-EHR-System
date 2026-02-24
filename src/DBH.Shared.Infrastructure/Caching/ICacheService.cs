namespace DBH.Shared.Infrastructure.Caching;

/// <summary>
/// Interface cho Distributed Cache Service
/// Abstraction layer trên Redis cho DBH-EHR System
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value từ cache
    /// </summary>
    /// <typeparam name="T">Type của value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Value hoặc default nếu không tồn tại</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set value vào cache với expiration
    /// </summary>
    /// <typeparam name="T">Type của value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Thời gian hết hạn (null = dùng default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create - Nếu không có trong cache thì gọi factory để tạo
    /// </summary>
    /// <typeparam name="T">Type của value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function để tạo value nếu chưa có</param>
    /// <param name="expiration">Thời gian hết hạn</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Value từ cache hoặc từ factory</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove key từ cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove multiple keys theo pattern
    /// </summary>
    /// <param name="pattern">Pattern (e.g., "user:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if key exists
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh expiration của key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="expiration">New expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RefreshAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hash operations - Set field in hash
    /// </summary>
    Task HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hash operations - Get field from hash
    /// </summary>
    Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hash operations - Get all fields
    /// </summary>
    Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment a counter
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pub/Sub - Publish message to channel
    /// </summary>
    Task PublishAsync(string channel, string message, CancellationToken cancellationToken = default);
}
