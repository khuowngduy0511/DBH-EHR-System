namespace DBH.Shared.Contracts.Events;

/// <summary>
/// Base class cho tất cả integration events
/// </summary>
public abstract class IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}

// ============================================================================
// User Events
// ============================================================================

public class UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty;
    public string? Did { get; init; }
    public Guid? OrganizationId { get; init; }
}

public class UserVerifiedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Did { get; init; } = string.Empty;
}

public class UserDeactivatedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public class UserLoggedInEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Did { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
}

// ============================================================================
// Consent Events
// ============================================================================

public class ConsentGrantedEvent : IntegrationEvent
{
    public string ConsentId { get; init; } = string.Empty;
    public string PatientDid { get; init; } = string.Empty;
    public string GranteeDid { get; init; } = string.Empty;
    public string GranteeType { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public string Purpose { get; init; } = string.Empty;
    public DateTime? ValidUntil { get; init; }
    public string? BlockchainTxHash { get; init; }
}

public class ConsentRevokedEvent : IntegrationEvent
{
    public string ConsentId { get; init; } = string.Empty;
    public string PatientDid { get; init; } = string.Empty;
    public string GranteeDid { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? BlockchainTxHash { get; init; }
}

public class AccessRequestCreatedEvent : IntegrationEvent
{
    public Guid RequestId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string RequesterDid { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string RequesterOrganization { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public List<string> RequestedPermissions { get; init; } = new();
}

public class AccessRequestApprovedEvent : IntegrationEvent
{
    public Guid RequestId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string RequesterDid { get; init; } = string.Empty;
    public string ConsentId { get; init; } = string.Empty;
}

public class AccessRequestDeniedEvent : IntegrationEvent
{
    public Guid RequestId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string RequesterDid { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

// ============================================================================
// EHR Events
// ============================================================================

public class EhrCreatedEvent : IntegrationEvent
{
    public Guid EhrId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string CreatedByDid { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string EhrType { get; init; } = string.Empty;
    public string FhirResourceType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ContentHash { get; init; } = string.Empty;
    public string? BlockchainTxHash { get; init; }
}

public class EhrUpdatedEvent : IntegrationEvent
{
    public Guid EhrId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string UpdatedByDid { get; init; } = string.Empty;
    public int Version { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
    public string? BlockchainTxHash { get; init; }
}

public class EhrAccessedEvent : IntegrationEvent
{
    public Guid EhrId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string AccessedByDid { get; init; } = string.Empty;
    public string AccessedByName { get; init; } = string.Empty;
    public Guid? AccessedByOrganizationId { get; init; }
    public string AccessType { get; init; } = "READ";
    public string Purpose { get; init; } = string.Empty;
    public string? ConsentId { get; init; }
}

public class EhrSharedEvent : IntegrationEvent
{
    public Guid EhrId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string SharedWithDid { get; init; } = string.Empty;
    public string SharedByDid { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
}

public class EhrFileUploadedEvent : IntegrationEvent
{
    public Guid FileId { get; init; }
    public Guid EhrId { get; init; }
    public string PatientDid { get; init; } = string.Empty;
    public string UploadedByDid { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}

// ============================================================================
// Organization Events
// ============================================================================

public class OrganizationCreatedEvent : IntegrationEvent
{
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Did { get; init; }
}

public class OrganizationVerifiedEvent : IntegrationEvent
{
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FabricMspId { get; init; } = string.Empty;
}

public class MembershipCreatedEvent : IntegrationEvent
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public string UserDid { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string Role { get; init; } = string.Empty;
}
