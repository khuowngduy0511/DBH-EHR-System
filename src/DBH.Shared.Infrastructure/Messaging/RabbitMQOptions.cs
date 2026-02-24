namespace DBH.Shared.Infrastructure.Messaging;

/// <summary>
/// Cấu hình RabbitMQ Message Queue
/// </summary>
public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ Host
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ Port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual Host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Enable RabbitMQ (false để dùng in-memory trong dev)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Retry count cho message delivery
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Retry interval in seconds
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Prefetch count (concurrent messages per consumer)
    /// </summary>
    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;
}
