using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Payment.Service.Models.Enums;
using DBH.Shared.Contracts;

namespace DBH.Payment.Service.Models.Entities;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("encounter_id")]
    public Guid? EncounterId { get; set; }

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(30)]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.UNPAID;

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTimeHelper.Now;

    // Navigation
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<Entities.Payment> Payments { get; set; } = new List<Entities.Payment>();
}
