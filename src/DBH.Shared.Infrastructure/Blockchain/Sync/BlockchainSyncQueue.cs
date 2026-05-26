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
    private static readonly string[] KnownBlockchainQueues =
    {
        MainQueue,
        "dbh.blockchain.sync.ehr.queue",
        "dbh.blockchain.sync.consent.queue",
        "dbh.blockchain.sync.audit.queue",
        "dbh.blockchain.sync.auth.queue"
    };
    private const string RetryHeader = "x-retry-count";
    private const string ErrorHeader = "x-error-message";

    private readonly RabbitMQOptions _rabbitOptions;
    private readonly ILogger<BlockchainSyncQueue> _logger;
    private readonly ConcurrentQueue<BlockchainSyncJob> _fallbackQueue = new();
    private readonly SemaphoreSlim _fallbackSignal = new(0);

    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _channelLock = new();

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
    /// Falls back to in-memory queue if RabbitMQ channel is unavailable.
    /// </summary>
    public void Enqueue(BlockchainSyncJob job)
    {
        if (_channel == null)
        {
            _fallbackQueue.Enqueue(job);
            _fallbackSignal.Release();
            return;
        }

        try
        {
            EnsureChannelOpen();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = new Dictionary<string, object>
            {
                [RetryHeader] = 0
            };

            var routingKey = GetRoutingKey(job.JobType);
            _channel.BasicPublish(MainExchange, routingKey, props, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RabbitMQ publish failed for job {JobType}/{EntityId}. Falling back to in-memory queue.",
                job.JobType, job.EntityId);
            _fallbackQueue.Enqueue(job);
            _fallbackSignal.Release();
        }
    }

    /// <summary>
    /// Checks if the RabbitMQ channel is still open. If not, attempts to recreate it.
    /// </summary>
    private void EnsureChannelOpen()
    {
        if (_channel != null && _channel.IsOpen)
            return;

        lock (_channelLock)
        {
            // Double-check after acquiring lock
            if (_channel != null && _channel.IsOpen)
                return;

            _logger.LogWarning("RabbitMQ channel is closed. Attempting to recreate...");

            try
            {
                // If connection is also dead, recreate it
                if (_connection == null || !_connection.IsOpen)
                {
                    _logger.LogWarning("RabbitMQ connection is also closed. Reconnecting...");
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
                }

                _channel = _connection!.CreateModel();

                // Re-declare exchanges and queues (idempotent)
                _channel.ExchangeDeclare(MainExchange, ExchangeType.Direct, durable: true);
                _channel.ExchangeDeclare(DlqExchange, ExchangeType.Direct, durable: true);
                _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        ["x-dead-letter-exchange"] = DlqExchange,
                        ["x-dead-letter-routing-key"] = DlqRoutingKey
                    });
                _channel.QueueDeclare(DlqQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                if (_allowedJobTypes != null && _allowedJobTypes.Any())
                {
                    foreach (var jobType in _allowedJobTypes)
                    {
                        _channel.QueueBind(_queueName, MainExchange, GetRoutingKey(jobType));
                    }
                }
                else
                {
                    _channel.QueueBind(_queueName, MainExchange, MainRoutingKey);
                }
                _channel.QueueBind(DlqQueue, DlqExchange, DlqRoutingKey);
                _channel.BasicQos(0, 1, false);

                _logger.LogInformation("RabbitMQ channel recreated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recreate RabbitMQ channel. Will use in-memory fallback.");
                _channel = null;
            }
        }
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
            try
            {
                EnsureChannelOpen();
                if (_channel == null)
                {
                    // Channel recreation failed, wait and retry
                    await Task.Delay(3000, ct);
                    continue;
                }

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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during RabbitMQ dequeue. Will retry after delay.");
                await Task.Delay(3000, ct);
                continue;
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
            try
            {
                _channel.BasicAck(dequeued.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ack delivery tag {Tag}. Message may be redelivered.", dequeued.DeliveryTag);
            }
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

        try
        {
            EnsureChannelOpen();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = new Dictionary<string, object>
            {
                [RetryHeader] = dequeued.RetryCount + 1
            };

            var routingKey = GetRoutingKey(job.JobType);
            _channel.BasicPublish(MainExchange, routingKey, props, body);
            _channel.BasicAck(dequeued.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ requeue failed for {JobType}/{EntityId}. Falling back to in-memory queue.",
                job.JobType, job.EntityId);
            _fallbackQueue.Enqueue(job);
            _fallbackSignal.Release();
        }

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

        try
        {
            EnsureChannelOpen();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = new Dictionary<string, object>
            {
                [RetryHeader] = dequeued.RetryCount,
                [ErrorHeader] = errorMessage ?? "Unknown error"
            };

            _channel.BasicPublish(DlqExchange, DlqRoutingKey, props, body);
            _channel.BasicAck(dequeued.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to move job {JobType}/{EntityId} to DLQ.",
                job.JobType, job.EntityId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Moves all messages currently in the main queue to the dead-letter queue.
    /// This is intended for emergency scenarios (for example when blockchain is unreachable)
    /// so that messages are preserved in the DLQ for later replay instead of being processed.
    /// </summary>
    public Task MoveAllToDeadLetterAsync(CancellationToken ct)
    {
        if (_channel == null)
        {
            return Task.CompletedTask;
        }

        while (!ct.IsCancellationRequested)
        {
            var result = _channel.BasicGet(_queueName, autoAck: false);
            if (result == null)
            {
                break; // no more messages
            }

            var bodyJson = Encoding.UTF8.GetString(result.Body.ToArray());
            BlockchainSyncJob? job = null;
            try
            {
                job = JsonSerializer.Deserialize<BlockchainSyncJob>(bodyJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize queued message while moving to DLQ; acking and skipping");
                try { _channel.BasicAck(result.DeliveryTag, multiple: false); } catch { }
                continue;
            }

            if (job == null)
            {
                _logger.LogWarning("Queued message deserialized to null while moving to DLQ; acking and skipping");
                try { _channel.BasicAck(result.DeliveryTag, multiple: false); } catch { }
                continue;
            }

            try
            {
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                props.Headers = new Dictionary<string, object>
                {
                    [RetryHeader] = ReadRetryCount(result.BasicProperties.Headers),
                    [ErrorHeader] = "Moved to DLQ due to blockchain unreachable at startup"
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
                _channel.BasicPublish(DlqExchange, DlqRoutingKey, props, body);

                // Ack original message after publishing to DLQ
                _channel.BasicAck(result.DeliveryTag, multiple: false);

                _logger.LogInformation("Moved queued job {JobId} Type={JobType} to DLQ", job.JobId, job.JobType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move queued job {JobId} to DLQ; requeuing into main queue", job.JobId);
                try { _channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true); } catch { }
                // break to avoid tight loop if publishing fails repeatedly
                break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Requeues messages from the dead-letter queue back to the main queue.
    /// If <paramref name="jobId"/> is provided only the matching job will be requeued;
    /// otherwise all messages from the DLQ will be republished to the main exchange.
    /// </summary>
    public Task RequeueFromDeadLetterAsync(string? jobId, CancellationToken ct)
    {
        if (_channel == null)
        {
            return Task.CompletedTask;
        }

        while (!ct.IsCancellationRequested)
        {
            var result = _channel.BasicGet(DlqQueue, autoAck: false);
            if (result == null)
            {
                break; // no more messages
            }

            var bodyJson = Encoding.UTF8.GetString(result.Body.ToArray());
            BlockchainSyncJob? job = null;
            try
            {
                job = JsonSerializer.Deserialize<BlockchainSyncJob>(bodyJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize DLQ message, acking and skipping");
                _channel.BasicAck(result.DeliveryTag, multiple: false);
                continue;
            }

            // If a specific jobId is requested and this isn't it, put it back to the DLQ
            if (!string.IsNullOrEmpty(jobId) && (job == null || job.JobId != jobId))
            {
                // requeue into DLQ
                _channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
                continue;
            }

            if (job == null)
            {
                _channel.BasicAck(result.DeliveryTag, multiple: false);
                continue;
            }

            // Republish to main exchange with retry header reset (or preserved if present)
            try
            {
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                props.Headers = new Dictionary<string, object>
                {
                    [RetryHeader] = 0
                };

                var routingKey = GetRoutingKey(job.JobType);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));
                _channel.BasicPublish(MainExchange, routingKey, props, body);

                // Ack the DLQ message after successful publish
                _channel.BasicAck(result.DeliveryTag, multiple: false);
                _logger.LogInformation("Requeued DLQ job {JobId} to main queue", job.JobId);

                // If a single job was requested, stop after requeuing it
                if (!string.IsNullOrEmpty(jobId))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to republish DLQ message for JobId={JobId}; requeuing into DLQ", job.JobId);
                try
                {
                    _channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
                }
                catch { }
                // Avoid tight loop on repeated failures
                break;
            }
        }

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

    /// <summary>
    /// Returns the total number of queued blockchain jobs across all known blockchain sync queues.
    /// This is useful for admin status checks that need a system-wide view rather than a single service queue.
    /// </summary>
    public int GetTotalQueuedJobs()
    {
        EnsureChannelOpen();
        if (_channel == null)
        {
            return _fallbackQueue.Count;
        }

        var queueNames = KnownBlockchainQueues
            .Append(_queueName)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var total = 0;
        foreach (var queueName in queueNames)
        {
            total += TryGetQueueMessageCount(queueName);
        }

        return total;
    }

    /// <summary>
    /// Returns counts of queued jobs per queue name.
    /// Uses passive queue lookup; falls back to in-memory queue count when channel is not available.
    /// </summary>
    public Dictionary<string, int> GetQueuedJobsByQueue()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        EnsureChannelOpen();
        if (_channel == null)
        {
            result[_queueName] = _fallbackQueue.Count;
            return result;
        }

        var queueNames = KnownBlockchainQueues
            .Append(_queueName)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var q in queueNames)
        {
            result[q] = TryGetQueueMessageCount(q);
        }

        return result;
    }

    /// <summary>
    /// Returns counts of dead-letter messages grouped by job type.
    /// Uses the existing GetDeadLetters helper which peeks DLQ messages.
    /// </summary>
    public Dictionary<string, int> GetDeadLetterCountsByJobType()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        EnsureChannelOpen();
        if (_channel == null)
        {
            return result;
        }

        var deadLetters = GetDeadLetters();
        var groups = deadLetters
            .Where(dl => dl.Job != null)
            .GroupBy(dl => dl.Job.JobType.ToString());

        foreach (var g in groups)
        {
            result[g.Key] = g.Count();
        }

        return result;
    }

    /// <summary>
    /// Returns counts of dead-letter messages grouped by inferred originating queue.
    /// The originating queue is inferred from the job type.
    /// </summary>
    public Dictionary<string, int> GetDeadLetterCountsByOriginQueue()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        EnsureChannelOpen();
        if (_channel == null)
        {
            return result;
        }

        var deadLetters = GetDeadLetters();
        foreach (var (job, _, _) in deadLetters)
        {
            if (job == null) continue;
            var queue = MapJobTypeToQueueName(job.JobType);
            if (!result.ContainsKey(queue)) result[queue] = 0;
            result[queue]++;
        }

        return result;
    }

    private static string MapJobTypeToQueueName(BlockchainSyncJobType jobType)
    {
        return jobType switch
        {
            BlockchainSyncJobType.EhrHash => "dbh.blockchain.sync.ehr.queue",
            BlockchainSyncJobType.ConsentGrant => "dbh.blockchain.sync.consent.queue",
            BlockchainSyncJobType.ConsentRevoke => "dbh.blockchain.sync.consent.queue",
            BlockchainSyncJobType.AuditLog => "dbh.blockchain.sync.audit.queue",
            BlockchainSyncJobType.FabricCaEnrollment => "dbh.blockchain.sync.auth.queue",
            _ => MainQueue
        };
    }

    /// <summary>
    /// Retrieves all messages from the dead-letter queue without consuming them.
    /// Returns a list of tuples containing (job, retryCount, errorMessage).
    /// </summary>
    public List<(BlockchainSyncJob Job, int RetryCount, string? ErrorMessage)> GetDeadLetters()
    {
        var result = new List<(BlockchainSyncJob, int, string?)>();

        EnsureChannelOpen();
        if (_channel == null)
        {
            return result;
        }

        // Use a temporary loop to peek at all DLQ messages without removing them
        var tempMessages = new List<(ulong deliveryTag, BlockchainSyncJob job, int retryCount, string? errorMessage)>();

        try
        {
            while (true)
            {
                var message = _channel.BasicGet(DlqQueue, autoAck: false);
                if (message == null)
                {
                    break;
                }

                var bodyJson = Encoding.UTF8.GetString(message.Body.ToArray());
                BlockchainSyncJob? job = null;
                try
                {
                    job = JsonSerializer.Deserialize<BlockchainSyncJob>(bodyJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize DLQ message");
                    _channel.BasicNack(message.DeliveryTag, multiple: false, requeue: true);
                    continue;
                }

                if (job != null)
                {
                    var retryCount = ReadRetryCount(message.BasicProperties.Headers);
                    var errorMessage = ReadErrorMessage(message.BasicProperties.Headers);
                    tempMessages.Add((message.DeliveryTag, job, retryCount, errorMessage));
                    result.Add((job, retryCount, errorMessage));
                }
                else
                {
                    _channel.BasicNack(message.DeliveryTag, multiple: false, requeue: true);
                }
            }
        }
        finally
        {
            // Put all messages back to the DLQ in the order we found them
            foreach (var (deliveryTag, _, _, _) in tempMessages)
            {
                try
                {
                    _channel.BasicNack(deliveryTag, multiple: false, requeue: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to requeue message during dead-letter peek");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the count of messages in the dead-letter queue.
    /// </summary>
    public int DeadLetterCount
    {
        get
        {
            EnsureChannelOpen();
            if (_channel == null)
            {
                return 0;
            }

            return (int)_channel.MessageCount(DlqQueue);
        }
    }

    private int TryGetQueueMessageCount(string queueName)
    {
        EnsureChannelOpen();
        if (_channel == null)
        {
            return 0;
        }

        try
        {
            var queueInfo = _channel.QueueDeclarePassive(queueName);
            return (int)queueInfo.MessageCount;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to read message count for queue {QueueName}", queueName);
            return 0;
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

    private static string? ReadErrorMessage(IDictionary<string, object>? headers)
    {
        if (headers == null || !headers.TryGetValue(ErrorHeader, out var value) || value == null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString()
        };
    }
}