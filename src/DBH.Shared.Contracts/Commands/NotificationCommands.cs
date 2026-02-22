namespace DBH.Shared.Contracts.Commands;

/// <summary>
/// Base class cho notification commands
/// </summary>
public abstract class NotificationCommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

// ============================================================================
// Push Notification Commands
// ============================================================================

public class SendPushNotificationCommand : NotificationCommand
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public Dictionary<string, string>? Data { get; init; }
    public string? ActionType { get; init; }
    public string? ActionId { get; init; }
}

public class SendBulkPushNotificationCommand : NotificationCommand
{
    public List<Guid> UserIds { get; init; } = new();
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, string>? Data { get; init; }
}

// ============================================================================
// Email Commands
// ============================================================================

public class SendEmailCommand : NotificationCommand
{
    public string ToEmail { get; init; } = string.Empty;
    public string? ToName { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? TemplateName { get; init; }
    public Dictionary<string, object>? TemplateData { get; init; }
    public string? HtmlBody { get; init; }
    public string? TextBody { get; init; }
}

public class SendEmailVerificationCommand : NotificationCommand
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
    public string VerificationUrl { get; init; } = string.Empty;
}

public class SendPasswordResetCommand : NotificationCommand
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public string ResetUrl { get; init; } = string.Empty;
}

// ============================================================================
// SMS Commands
// ============================================================================

public class SendSmsCommand : NotificationCommand
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class SendMfaCodeCommand : NotificationCommand
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string MfaCode { get; init; } = string.Empty;
}

// ============================================================================
// Specific Notification Commands
// ============================================================================

public class NotifyAccessRequestCommand : NotificationCommand
{
    public Guid PatientUserId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string RequesterOrganization { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public Guid RequestId { get; init; }
}

public class NotifyConsentGrantedCommand : NotificationCommand
{
    public Guid GranteeUserId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public DateTime? ValidUntil { get; init; }
}

public class NotifyEhrAccessedCommand : NotificationCommand
{
    public Guid PatientUserId { get; init; }
    public string AccessedByName { get; init; } = string.Empty;
    public string AccessedByOrganization { get; init; } = string.Empty;
    public string EhrTitle { get; init; } = string.Empty;
    public DateTime AccessedAt { get; init; }
}

public class NotifyNewEhrRecordCommand : NotificationCommand
{
    public Guid PatientUserId { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string EhrType { get; init; } = string.Empty;
    public string EhrTitle { get; init; } = string.Empty;
    public Guid EhrId { get; init; }
}
