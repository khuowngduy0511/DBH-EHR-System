using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.Entities;

/// <summary>
/// Subscription quản lý EHR 
/// </summary>
[Table("ehr_subscriptions")]
public class EhrSubscription
{
    [Key]
    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("ehr_id")]
    public Guid? EhrId { get; set; }

    [Column("plan_id")]
    public Guid? PlanId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; } = false;

    [Column("status")]
    [MaxLength(20)]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.ACTIVE;

    [Column("next_billing_date")]
    public DateOnly? NextBillingDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
