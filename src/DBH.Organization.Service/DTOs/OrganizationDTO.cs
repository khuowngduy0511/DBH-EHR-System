using System.ComponentModel.DataAnnotations;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.DTOs;

public class CreateOrganizationRequest
{
    [Required]
    [MaxLength(255)]
    public string OrgName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? OrgCode { get; set; }

    public OrganizationType OrgType { get; set; } = OrganizationType.HOSPITAL;

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// JSON format: { "line": ["123 Street"], "city": "Ho Chi Minh", "district": "Quan 1", "country": "VN" }
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// JSON format: { "phone": "028-xxx", "email": "contact@hospital.vn" }
    /// </summary>
    public string? ContactInfo { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }
}

public class UpdateOrganizationRequest
{
    [MaxLength(255)]
    public string? OrgName { get; set; }

    [MaxLength(50)]
    public string? OrgCode { get; set; }

    public OrganizationType? OrgType { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? ContactInfo { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public string? Settings { get; set; }
}

public class OrganizationResponse
{
    public Guid OrgId { get; set; }
    public string? OrgDid { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? OrgCode { get; set; }
    public OrganizationType OrgType { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public OrganizationStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FabricMspId { get; set; }
    public string? FabricChannelPeers { get; set; }
    public string? FabricCaUrl { get; set; }
    public int DepartmentCount { get; set; }
    public int MemberCount { get; set; }
}

public class UpdateOrganizationFabricConfigRequest
{
    [MaxLength(100)]
    public string? FabricMspId { get; set; }

    /// <summary>
    /// JSON array or object describing channel peers, stored as JSON string
    /// </summary>
    public string? FabricChannelPeers { get; set; }

    [MaxLength(500)]
    public string? FabricCaUrl { get; set; }
}