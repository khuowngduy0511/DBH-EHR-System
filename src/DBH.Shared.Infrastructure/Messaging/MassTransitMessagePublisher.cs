using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DBH.Shared.Infrastructure.Messaging;

/// <summary>
/// MassTransit-based Message Publisher for RabbitMQ
/// </summary>
public class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly IBus _bus;
    private readonly ILogger<MassTransitMessagePublisher> _logger;
    private readonly RabbitMQOptions _options;

    public MassTransitMessagePublisher(
        IBus bus,
        ILogger<MassTransitMessagePublisher> logger,
        IOptions<RabbitMQOptions> options)
    {
        _bus = bus;
        _logger = logger;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _bus.Publish(message, cancellationToken);
            _logger.LogDebug("Published message of type {MessageType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }

    public async Task SendAsync<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var endpoint = await _bus.GetSendEndpoint(destinationAddress);
            await endpoint.Send(message, cancellationToken);
            _logger.LogDebug("Sent message of type {MessageType} to {Destination}", typeof(T).Name, destinationAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message of type {MessageType} to {Destination}", typeof(T).Name, destinationAddress);
            throw;
        }
    }

    public async Task SendToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        var uri = new Uri($"queue:{queueName}");
        await SendAsync(uri, message, cancellationToken);
    }

    public async Task ScheduleAsync<T>(T message, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Schedule message using delayed exchange or in-memory scheduler
            // Note: Requires RabbitMQ delayed message exchange plugin in production
            var delay = scheduledTime - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
            await PublishAsync(message, cancellationToken);
            _logger.LogDebug("Scheduled message of type {MessageType} for {ScheduledTime}", typeof(T).Name, scheduledTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }
}
