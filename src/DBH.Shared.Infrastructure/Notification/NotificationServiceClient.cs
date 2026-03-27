using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DBH.Shared.Infrastructure.Notification;

/// <summary>
/// HTTP client gọi Notification Service API (POST /api/v1/notifications).
/// Fire-and-forget: log warning nếu gửi thất bại, không throw exception.
/// </summary>
public class NotificationServiceClient : INotificationServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public NotificationServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid recipientUserId,
        string title,
        string body,
        string type,
        string priority = "Normal",
        string? referenceId = null,
        string? referenceType = null,
        string? actionUrl = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("NotificationService");

            var payload = new
            {
                RecipientDid = $"did:user:{recipientUserId}",
                RecipientUserId = recipientUserId,
                Title = title,
                Body = body,
                Type = type,
                Priority = priority,
                Channel = "InApp",
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                ActionUrl = actionUrl
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/v1/notifications", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Notification sent: type={Type} to={UserId} title={Title}",
                    type, recipientUserId, title);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Notification Service returned {StatusCode} for type={Type} to={UserId}: {Error}",
                    response.StatusCode, type, recipientUserId, errorBody);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: log warning, không throw
            _logger.LogWarning(ex,
                "Failed to send notification type={Type} to={UserId}. Notification Service may be unavailable.",
                type, recipientUserId);
        }
    }
}
