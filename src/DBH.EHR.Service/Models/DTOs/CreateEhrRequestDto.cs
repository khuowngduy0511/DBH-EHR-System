using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DBH.EHR.Service.Models.DTOs;

/// <summary>
/// Request DTO for creating a new EHR change request
/// </summary>
public class CreateEhrRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Purpose { get; set; } = string.Empty;

    [StringLength(200)]
    public string? RequestedScope { get; set; }

    [Range(1, 10080)] // Max 7 days
    public int TtlMinutes { get; set; } = 60;

    [StringLength(100)]
    public string? RecordType { get; set; }

    /// <summary>
    /// The EHR document content (FHIR-like JSON)
    /// </summary>
    [Required]
    public JsonElement Document { get; set; }
}

/// <summary>
/// Response DTO after creating a change request
/// </summary>
public class CreateEhrResponseDto
{
    public Guid ChangeRequestId { get; set; }
    public string? OffchainDocId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Metadata showing which nodes served the write
    public WriteNodeMetadata WriteToNode { get; set; } = new();
}

public class WriteNodeMetadata
{
    public string PostgresNode { get; set; } = "primary";
    public string MongoNode { get; set; } = "primary";
}
