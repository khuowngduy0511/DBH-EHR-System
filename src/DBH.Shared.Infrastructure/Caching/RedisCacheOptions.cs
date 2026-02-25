namespace DBH.Shared.Infrastructure.Caching;

/// <summary>
/// Cấu hình Redis Cache
/// </summary>
public class RedisCacheOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string (e.g., "localhost:6379,password=xxx")
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Instance name prefix cho cache keys
    /// </summary>
    public string InstanceName { get; set; } = "DBH-EHR:";

    /// <summary>
    /// Default expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Enable Redis (false để fallback sang in-memory cache trong dev)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// SSL/TLS enabled
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Connect timeout in milliseconds
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Allow admin operations
    /// </summary>
    public bool AllowAdmin { get; set; } = false;

    /// <summary>
    /// Database index (0-15)
    /// </summary>
    public int Database { get; set; } = 0;
}
