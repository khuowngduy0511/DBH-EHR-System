using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.EHR.Service.Models.Entities;


[Table("change_requests")]
public class ChangeRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    [MaxLength(100)]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [Column("purpose")]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [Column("requested_scope")]
    [MaxLength(200)]
    public string? RequestedScope { get; set; }

    [Column("ttl_minutes")]
    public int TtlMinutes { get; set; } = 60;

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public RequestStatus Status { get; set; } = RequestStatus.PENDING;

 
    [Column("approvals", TypeName = "jsonb")]
    public string Approvals { get; set; } = "[]";

    [Column("offchain_doc_id")]
    [MaxLength(100)]
    public string? OffchainDocId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public List<string> ApprovalsList
    {
        get => string.IsNullOrEmpty(Approvals) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(Approvals) ?? new List<string>();
        set => Approvals = System.Text.Json.JsonSerializer.Serialize(value);
    }
}

public enum RequestStatus
{
    PENDING,
    APPROVED,
    APPLIED,
    REJECTED
}
