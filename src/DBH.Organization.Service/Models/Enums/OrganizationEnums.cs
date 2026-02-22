namespace DBH.Organization.Service.Models.Enums;

/// <summary>
/// Loại tổ chức y tế
/// </summary>
public enum OrganizationType
{
    HOSPITAL,
    CLINIC,
    LAB,
    PHARMACY,
    IMAGING_CENTER,
    HEALTH_CENTER
}

/// <summary>
/// Trạng thái tổ chức
/// </summary>
public enum OrganizationStatus
{
    ACTIVE,
    SUSPENDED,
    INACTIVE,
    PENDING_VERIFICATION
}

/// <summary>
/// Trạng thái thành viên trong tổ chức
/// </summary>
public enum MembershipStatus
{
    ACTIVE,
    ON_LEAVE,
    TERMINATED,
    PENDING
}

/// <summary>
/// Trạng thái phòng ban
/// </summary>
public enum DepartmentStatus
{
    ACTIVE,
    INACTIVE
}
