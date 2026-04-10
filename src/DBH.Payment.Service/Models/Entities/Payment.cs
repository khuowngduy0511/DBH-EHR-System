using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Payment.Service.Models.Enums;

namespace DBH.Payment.Service.Models.Entities;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("payment_id")]
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Required]
    [Column("method")]
    [MaxLength(50)]
    public PaymentMethod Method { get; set; }

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(30)]
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    [Column("transaction_ref")]
    [MaxLength(255)]
    public string? TransactionRef { get; set; }

    [Column("order_code")]
    public long OrderCode { get; set; }

    [Column("payment_link_id")]
    [MaxLength(255)]
    public string? PaymentLinkId { get; set; }

    [Column("checkout_url")]
    [MaxLength(2000)]
    public string? CheckoutUrl { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice Invoice { get; set; } = null!;
}
