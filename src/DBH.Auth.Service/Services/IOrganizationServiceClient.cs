namespace DBH.Auth.Service.Services;

/// <summary>
/// Client for communicating with Organization Service
/// </summary>
public interface IOrganizationServiceClient
{
    /// <summary>
    /// Creates a membership record for a user in an organization
    /// </summary>
    Task<OrganizationServiceResponse<CreateMembershipResponse>> CreateMembershipAsync(
        Guid userId, 
        Guid organizationId, 
        Guid? departmentId = null,
        string? jobTitle = null);
}

public class CreateMembershipResponse
{
    public Guid MembershipId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrgId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class OrganizationServiceResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
}
