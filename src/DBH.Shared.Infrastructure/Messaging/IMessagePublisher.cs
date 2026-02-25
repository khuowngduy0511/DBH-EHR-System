namespace DBH.Shared.Infrastructure.Messaging;

/// <summary>
/// Interface cho Message Publisher
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish message to exchange
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="message">Message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Send message to specific endpoint
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="destinationAddress">Destination queue URI</param>
    /// <param name="message">Message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Send message to specific queue by name
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="queueName">Queue name</param>
    /// <param name="message">Message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Schedule message for later delivery
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="message">Message to schedule</param>
    /// <param name="scheduledTime">Time to deliver message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ScheduleAsync<T>(T message, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default) where T : class;
}
