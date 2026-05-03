using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.DbContext;

/// <summary>
/// PostgreSQL Primary - Đọc/Ghi
/// </summary>
public class EhrPrimaryDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public EhrPrimaryDbContext(DbContextOptions<EhrPrimaryDbContext> options) 
        : base(options)
    {
    }

    // Bảng EHR
    public DbSet<EhrRecord> EhrRecords => Set<EhrRecord>();
    public DbSet<EhrVersion> EhrVersions => Set<EhrVersion>();
    public DbSet<EhrFile> EhrFiles => Set<EhrFile>();
    public DbSet<EhrSubscription> EhrSubscriptions => Set<EhrSubscription>();
    public DbSet<EhrAccessLog> EhrAccessLogs => Set<EhrAccessLog>();

    // Bảng Lab Orders
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EhrRecord>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.CreatedAt });
            entity.HasIndex(e => e.EncounterId);
            entity.HasIndex(e => new { e.OrgId, e.CreatedAt });
        });

        modelBuilder.Entity<EhrVersion>(entity =>
        {
            entity.HasIndex(e => new { e.EhrId, e.VersionNumber }).IsUnique();

            entity.HasOne(e => e.EhrRecord)
                .WithMany(r => r.Versions)
                .HasForeignKey(e => e.EhrId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EhrFile>(entity =>
        {
            entity.HasIndex(e => e.EhrId);

            entity.HasOne(e => e.EhrRecord)
                .WithMany(r => r.Files)
                .HasForeignKey(e => e.EhrId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EhrSubscription>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.Status });
            entity.HasIndex(e => new { e.EhrId, e.Status });
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.EhrRecord)
                .WithMany()
                .HasForeignKey(e => e.EhrId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EhrAccessLog>(entity =>
        {
            entity.HasIndex(e => new { e.EhrId, e.AccessedAt });
            entity.HasIndex(e => new { e.AccessedBy, e.AccessedAt });
            
            entity.Property(e => e.AccessAction)
                .HasConversion<string>()
                .HasMaxLength(50);
                
            entity.Property(e => e.VerifyStatus)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.HasOne(e => e.EhrRecord)
                .WithMany()
                .HasForeignKey(e => e.EhrId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabOrder>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.Status });
            entity.HasIndex(e => new { e.OrgId, e.Status });
            entity.HasIndex(e => e.RequestedBy);
            entity.HasIndex(e => e.EhrId);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.EhrRecord)
                .WithMany()
                .HasForeignKey(e => e.EhrId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
