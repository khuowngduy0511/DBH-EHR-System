using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Blockchain;
using DBH.Shared.Infrastructure;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Tests for BlockchainSyncQueue — focusing on the DLQ configuration bug fix.
/// 
/// Root cause: The Blockchain Service was passing GetSection("HyperledgerFabric")
/// to AddHyperledgerFabric, causing RabbitMQ options to never bind.
/// This made BlockchainSyncQueue fall back to in-memory mode where
/// DeadLetterCount always returns 0 and GetDeadLetters returns empty.
/// </summary>
public class BlockchainSyncQueueTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<BlockchainSyncQueue> _logger;

    public BlockchainSyncQueueTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new Mock<ILogger<BlockchainSyncQueue>>().Object;
    }

    // =========================================================================
    // T1: RabbitMQ disabled → in-memory fallback, DeadLetterCount = 0
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::RabbitMQ disabled → DeadLetterCount returns 0")]
    public void DeadLetterCount_WhenRabbitMQDisabled_ReturnsZero()
    {
        // Arrange: RabbitMQ explicitly disabled
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        // Act
        var dlqCount = queue.DeadLetterCount;

        // Assert
        Assert.Equal(0, dlqCount);
        _output.WriteLine($"DeadLetterCount with RabbitMQ disabled: {dlqCount}");
    }

    [Fact(DisplayName = "BlockchainSyncQueue::RabbitMQ disabled → GetDeadLetters returns empty")]
    public void GetDeadLetters_WhenRabbitMQDisabled_ReturnsEmpty()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        // Act
        var deadLetters = queue.GetDeadLetters();

        // Assert
        Assert.NotNull(deadLetters);
        Assert.Empty(deadLetters);
        _output.WriteLine($"GetDeadLetters with RabbitMQ disabled: {deadLetters.Count} items");
    }

    // =========================================================================
    // T2: RabbitMQ enabled but unreachable → falls back gracefully, Count = 0
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::Unreachable RabbitMQ → falls back to in-memory")]
    public void Constructor_WhenRabbitMQUnreachable_FallsBackToInMemory()
    {
        // Arrange: RabbitMQ enabled but wrong host (simulates Blockchain Service's original bug
        // where RabbitMQ options defaulted to localhost inside a Docker container)
        var options = Options.Create(new RabbitMQOptions
        {
            Enabled = true,
            Host = "nonexistent-host-12345",
            Port = 5672,
            Username = "guest",
            Password = "guest",
            ConnectionTimeoutSeconds = 2
        });

        // Act: Constructor should NOT throw
        var queue = new BlockchainSyncQueue(options, _logger);

        // Assert: Should fall back gracefully
        Assert.Equal(0, queue.Count);
        Assert.Equal(0, queue.DeadLetterCount);
        _output.WriteLine("BlockchainSyncQueue created with unreachable host — fell back to in-memory");
    }

    // =========================================================================
    // T3: In-memory fallback Enqueue/Count works
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::In-memory enqueue increments Count")]
    public void Enqueue_WhenInMemoryMode_IncrementsCount()
    {
        // Arrange: No RabbitMQ connection (disabled)
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        var job = new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.EhrHash,
            EntityId = "test-ehr-001",
            PayloadJson = "{\"ehrId\": \"test-ehr-001\"}"
        };

        // Act
        queue.Enqueue(job);

        // Assert
        Assert.Equal(1, queue.Count);
        _output.WriteLine($"In-memory queue count after enqueue: {queue.Count}");
    }

    [Fact(DisplayName = "BlockchainSyncQueue::In-memory dequeue returns enqueued job")]
    public async Task DequeueAsync_WhenInMemoryMode_ReturnsEnqueuedJob()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        var job = new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.ConsentGrant,
            EntityId = "test-consent-001",
            PayloadJson = "{\"consentId\": \"test-consent-001\"}"
        };
        queue.Enqueue(job);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var dequeued = await queue.DequeueAsync(cts.Token);

        // Assert
        Assert.NotNull(dequeued);
        Assert.NotNull(dequeued.Job);
        Assert.Equal("test-consent-001", dequeued.Job.EntityId);
        Assert.Equal(BlockchainSyncJobType.ConsentGrant, dequeued.Job.JobType);
        _output.WriteLine($"Dequeued job: {dequeued.Job.JobType}/{dequeued.Job.EntityId}");
    }

    // =========================================================================
    // T4: Configuration binding — root config vs subsection
    // This is the EXACT bug that caused the Blockchain Service issue
    // =========================================================================

    [Fact(DisplayName = "AddHyperledgerFabric::Root config binds RabbitMQ correctly")]
    public void AddHyperledgerFabric_WithRootConfig_BindsRabbitMQOptions()
    {
        // Arrange: Simulate the CORRECT call (root configuration)
        var configDict = new Dictionary<string, string?>
        {
            ["RabbitMQ:Host"] = "test-rabbitmq-host",
            ["RabbitMQ:Port"] = "5672",
            ["RabbitMQ:Username"] = "admin",
            ["RabbitMQ:Password"] = "secret",
            ["RabbitMQ:Enabled"] = "true",
            ["HyperledgerFabric:Enabled"] = "false",
            ["HyperledgerFabric:SimulationMode"] = "true"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act: Pass ROOT config (the fix)
        services.AddHyperledgerFabric(config);

        var sp = services.BuildServiceProvider();
        var rabbitOptions = sp.GetRequiredService<IOptions<RabbitMQOptions>>();

        // Assert: RabbitMQ options should be correctly bound
        Assert.Equal("test-rabbitmq-host", rabbitOptions.Value.Host);
        Assert.Equal(5672, rabbitOptions.Value.Port);
        Assert.Equal("admin", rabbitOptions.Value.Username);
        Assert.Equal("secret", rabbitOptions.Value.Password);
        Assert.True(rabbitOptions.Value.Enabled);
        _output.WriteLine($"RabbitMQ Host resolved: {rabbitOptions.Value.Host} ✓");
    }

    [Fact(DisplayName = "AddHyperledgerFabric::Subsection config FAILS to bind RabbitMQ (reproduces bug)")]
    public void AddHyperledgerFabric_WithSubsectionConfig_FailsToBindRabbitMQ()
    {
        // Arrange: Simulate the BROKEN call (subsection as config root)
        var configDict = new Dictionary<string, string?>
        {
            ["RabbitMQ:Host"] = "correct-rabbitmq-host",
            ["RabbitMQ:Port"] = "5672",
            ["RabbitMQ:Username"] = "admin",
            ["RabbitMQ:Password"] = "secret",
            ["RabbitMQ:Enabled"] = "true",
            ["HyperledgerFabric:Enabled"] = "false",
            ["HyperledgerFabric:SimulationMode"] = "true"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act: Pass SUBSECTION (the bug — looks for "HyperledgerFabric:RabbitMQ" which doesn't exist)
        services.AddHyperledgerFabric(config.GetSection("HyperledgerFabric"));

        var sp = services.BuildServiceProvider();
        var rabbitOptions = sp.GetRequiredService<IOptions<RabbitMQOptions>>();

        // Assert: RabbitMQ host should NOT be the correct value (it falls back to default "localhost")
        Assert.NotEqual("correct-rabbitmq-host", rabbitOptions.Value.Host);
        Assert.Equal("localhost", rabbitOptions.Value.Host); // Default value from RabbitMQOptions
        _output.WriteLine($"RabbitMQ Host with subsection (BUG): '{rabbitOptions.Value.Host}' — defaults to localhost ✗");
    }

    // =========================================================================
    // T5: GetDeadLetterCountsByJobType returns empty when no channel
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::GetDeadLetterCountsByJobType returns empty when no channel")]
    public void GetDeadLetterCountsByJobType_WhenNoChannel_ReturnsEmptyDictionary()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        // Act
        var result = queue.GetDeadLetterCountsByJobType();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _output.WriteLine("GetDeadLetterCountsByJobType with no channel: empty dictionary");
    }

    // =========================================================================
    // T6: GetQueuedJobsByQueue returns in-memory count when no channel
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::GetQueuedJobsByQueue returns in-memory count when no channel")]
    public void GetQueuedJobsByQueue_WhenNoChannel_ReturnsInMemoryCount()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.AuditLog,
            EntityId = "audit-001"
        });

        // Act
        var result = queue.GetQueuedJobsByQueue();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("dbh.blockchain.sync.queue"));
        Assert.Equal(1, result["dbh.blockchain.sync.queue"]);
        _output.WriteLine($"GetQueuedJobsByQueue in-memory: {result["dbh.blockchain.sync.queue"]} job(s)");
    }

    // =========================================================================
    // T7: MoveToDeadLetterAsync is no-op when no channel
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::MoveToDeadLetterAsync is no-op when no channel")]
    public async Task MoveToDeadLetterAsync_WhenNoChannel_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        var dequeued = new BlockchainSyncDequeuedItem
        {
            Job = new BlockchainSyncJob
            {
                JobType = BlockchainSyncJobType.EhrHash,
                EntityId = "test-001"
            },
            DeliveryTag = 0,
            RetryCount = 3
        };

        // Act & Assert: Should not throw
        await queue.MoveToDeadLetterAsync(dequeued, dequeued.Job, "Test error", CancellationToken.None);
        Assert.Equal(0, queue.DeadLetterCount);
        _output.WriteLine("MoveToDeadLetterAsync with no channel: completed without throwing");
    }

    // =========================================================================
    // T8: RequeueFromDeadLetterAsync is no-op when no channel
    // =========================================================================

    [Fact(DisplayName = "BlockchainSyncQueue::RequeueFromDeadLetterAsync is no-op when no channel")]
    public async Task RequeueFromDeadLetterAsync_WhenNoChannel_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RabbitMQOptions { Enabled = false });
        var queue = new BlockchainSyncQueue(options, _logger);

        // Act & Assert
        await queue.RequeueFromDeadLetterAsync(null, CancellationToken.None);
        await queue.RequeueFromDeadLetterAsync("some-job-id", CancellationToken.None);
        _output.WriteLine("RequeueFromDeadLetterAsync with no channel: completed without throwing");
    }
}
