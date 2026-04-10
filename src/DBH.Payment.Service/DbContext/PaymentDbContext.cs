using DBH.Payment.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Payment.Service.DbContext;

public class PaymentDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
    public DbSet<Models.Entities.Payment> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Models.Entities.Payment>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.PatientId);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.OrgId);

        modelBuilder.Entity<Models.Entities.Payment>()
            .HasIndex(p => p.OrderCode)
            .IsUnique();
    }
}
