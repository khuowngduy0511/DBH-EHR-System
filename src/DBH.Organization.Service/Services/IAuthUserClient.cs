using DBH.Organization.Service.DTOs;

namespace DBH.Organization.Service.Services;

public interface IAuthUserClient
{
    Task<DoctorUserInfoDto?> GetDoctorByUserIdInMyOrganizationAsync(string bearerToken, Guid orgId, Guid userId);
    Task<Guid?> GetUserIdByPatientIdAsync(string bearerToken, Guid patientId);
    Task<Guid?> GetUserIdByDoctorIdAsync(string bearerToken, Guid doctorId);
    Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(string bearerToken, Guid userId);
    Task<List<Guid>> SearchUserIdsAsync(string bearerToken, string keyword);
}

public class DoctorUserInfoDto
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? OrganizationId { get; set; }
    public string? Status { get; set; }
}
