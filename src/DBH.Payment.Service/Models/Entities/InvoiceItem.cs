using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.Payment.Service.Models.Entities;

[Table("invoice_items")]
public class InvoiceItem
{
    [Key]
    [Column("item_id")]
    public Guid ItemId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("ehr_id")]
    public Guid? EhrId { get; set; }

    [Required]
    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Navigation
    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice Invoice { get; set; } = null!;
}
