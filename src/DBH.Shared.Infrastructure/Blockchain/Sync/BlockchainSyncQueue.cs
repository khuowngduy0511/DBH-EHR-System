 using DBH.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DBH.Shared.Infrastructure.Blockchain.Sync;

/// <summary>
/// RabbitMQ-backed queue for blockchain sync jobs.
/// It supports publish, ack, retry, and dead-letter publishing.
/// </summary>
public class BlockchainSyncQueue
{
    private const string MainExchange = "dbh.blockchain.sync.exchange";
    private const string MainQueue = "dbh.blockchain.sync.queue";
    private const string MainRoutingKey = "dbh.blockchain.sync";
    private const string DlqExchange = "dbh.blockchain.sync.dlx";
    private const string DlqQueue = "dbh.blockchain.sync.dlq";
    private const string DlqRoutingKey = "dbh.blockchain.sync.dead";
    private const string RetryHeader = "x-retry-count";
    private const string ErrorHeader = "x-error-message";

    private readonly RabbitMQOptions _rabbitOptions;
    private readonly ILogger<BlockchainSyncQueue> _logger;
    private readonly ConcurrentQueue<BlockchainSyncJob> _fallbackQueue = new();
    private readonly SemaphoreSlim _fallbackSignal = new(0);

    private readonly IConnection? _connection;
    private readonly IModel? _channel;

    private readonly string _queueName;
    private readonly IEnumerable<BlockchainSyncJobType>? _allowedJobTypes;

    public BlockchainSyncQueue(
        IOptions<RabbitMQOptions> rabbitOptions,
        ILogger<BlockchainSyncQueue> logger,
        string queueName = MainQueue,
        IEnumerable<BlockchainSyncJobType>? allowedJobTypes = null)
    {
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
        _queueName = queueName;
        _allowedJobTypes = allowedJobTypes;

        if (!_rabbitOptions.Enabled)
        {
            _logger.LogWarning("RabbitMQ is disabled for blockchain sync queue. Falling back to in-memory queue.");
            return;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitOptions.Host,
                Port = _rabbitOptions.Port,
                VirtualHost = _rabbitOptions.VirtualHost,
                UserName = _rabbitOptions.Username,
                Password = _rabbitOptions.Password,
                DispatchConsumersAsync = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(_rabbitOptions.ConnectionTimeoutSeconds)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(MainExchange, ExchangeType.Direct, durable: true);
            _channel.ExchangeDeclare(DlqExchange, ExchangeType.Direct, durable: true);

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = DlqExchange,
                    ["x-dead-letter-routing-key"] = DlqRoutingKey
                });

            _channel.QueueDeclare(
                queue: DlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind to specific job types if specified, otherwise bind to legacy routing key
            if (_allowedJobTypes != null && _allowedJobTypes.Any())
            {
                foreach (var jobType in _allowedJobTypes)
                {
                    var routingKey = GetRoutingKey(jobType);
                    _channel.QueueBind(_queueName, MainExchange, routingKey);
                    _logger.LogInformation("Bound queue {Queue} to routing key {Key}", _queueName, routingKey);
                }
            }
            else
            {
                _channel.QueueBind(_queueName, MainExchange, MainRoutingKey);
            }

            _channel.QueueBind(DlqQueue, DlqExchange, DlqRoutingKey);
            _channel.BasicQos(0, 1, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for blockchain sync queue. Falling back to in-memory queue.");
        }
    }

    private static string GetRoutingKey(BlockchainSyncJobType jobType) => $"dbh.blockchain.sync.{jobType.ToString().ToLower()}";

    /// <summary>
    /// Publishes a blockchain job to the main queue.
    /// </summary>
    public void Enqueue(BlockchainSyncJob job)
    {
        if (_channel == null)
        {
            _fallbackQueue.Enqueue(job);
            _fallbackSignal.Release();
            return;
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object>
        {
            [RetryHeader] = 0
        };

        var routingKey = GetRoutingKey(job.JobType);
        _channel.BasicPublish(MainExchange, routingKey, props, body);
    }

    /// <summary>
    /// Retrieves the next job from the queue.
    /// </summary>
    public async Task<BlockchainSyncDequeuedItem?> DequeueAsync(CancellationToken ct)
    {
        if (_channel == null)
        {
            await _fallbackSignal.WaitAsync(ct);
            _fallbackQueue.TryDequeue(out var fallbackJob);
            return fallbackJob == null
                ? null
                : new BlockchainSyncDequeuedItem
                {
                    Job = fallbackJob,
                    RetryCount = fallbackJob.Attempts
                };
        }

        while (!ct.IsCancellationRequested)
        {
            var result = _channel.BasicGet(_queueName, autoAck: false);
            if (result != null)
            {
                var bodyJson = Encoding.UTF8.GetString(result.Body.ToArray());
                var job = JsonSerializer.Deserialize<BlockchainSyncJob>(bodyJson);
                if (job == null)
                {
                    _channel.BasicAck(result.DeliveryTag, multiple: false);
                    return null;
                }

                return new BlockchainSyncDequeuedItem
                {
                    Job = job,
                    DeliveryTag = result.DeliveryTag,
                    RetryCount = ReadRetryCount(result.BasicProperties.Headers)
                };
            }

            await Task.Delay(300, ct);
        }

        return null;
    }

    /// <summary>
    /// Acknowledges successful processing of a queue message.
    /// </summary>
    public Task AckAsync(BlockchainSyncDequeuedItem dequeued, CancellationToken ct)
    {
        if (_channel != null && dequeued.DeliveryTag > 0)
        {
            _channel.BasicAck(dequeued.DeliveryTag, multiple: false);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-publishes a failed job back to the main queue with an incremented retry counter.
    /// </summary>
    public Task RequeueAsync(BlockchainSyncDequeuedItem dequeued, BlockchainSyncJob job, CancellationToken ct)
    {
        if (_channel == null)
        {
            _fallbackQueue.Enqueue(job);
            _fallbackSignal.Release();
            return Task.CompletedTask;
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object>
        {
            [RetryHeader] = dequeued.RetryCount + 1
        };

        var routingKey = GetRoutingKey(job.JobType);
        _channel.BasicPublish(MainExchange, routingKey, props, body);
        _channel.BasicAck(dequeued.DeliveryTag, multiple: false);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a permanently failed job to the dead-letter queue.
    /// </summary>
    public Task MoveToDeadLetterAsync(
        BlockchainSyncDequeuedItem dequeued,
        BlockchainSyncJob job,
        string? errorMessage,
        CancellationToken ct)
    {
        if (_channel == null)
        {
            return Task.CompletedTask;
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object>
        {
            [RetryHeader] = dequeued.RetryCount,
            [ErrorHeader] = errorMessage ?? "Unknown error"
        };

        _channel.BasicPublish(DlqExchange, DlqRoutingKey, props, body);
        _channel.BasicAck(dequeued.DeliveryTag, multiple: false);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the number of queued blockchain jobs.
    /// </summary>
    public int Count
    {
        get
        {
            if (_channel == null)
            {
                return _fallbackQueue.Count;
            }

            return (int)_channel.MessageCount(_queueName);
        }
    }

    private static int ReadRetryCount(IDictionary<string, object>? headers)
    {
        if (headers == null || !headers.TryGetValue(RetryHeader, out var value) || value == null)
        {
            return 0;
        }

        return value switch
        {
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 0
        };
    }
}