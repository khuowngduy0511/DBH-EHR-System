using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Data;

/// <summary>
/// PostgreSQL Replica - Chỉ đọc 
/// </summary>
public class EhrReplicaDbContext : DbContext
{
    public EhrReplicaDbContext(DbContextOptions<EhrReplicaDbContext> options) 
        : base(options)
    {
    }

    // Bảng EHR 
    public DbSet<EhrRecord> EhrRecords => Set<EhrRecord>();
    public DbSet<EhrVersion> EhrVersions => Set<EhrVersion>();
    public DbSet<EhrFile> EhrFiles => Set<EhrFile>();
    public DbSet<EhrSubscription> EhrSubscriptions => Set<EhrSubscription>();
    public DbSet<EhrAccessLog> EhrAccessLogs => Set<EhrAccessLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EhrRecord>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.CreatedAt });
            entity.HasIndex(e => e.EncounterId);
            entity.HasIndex(e => new { e.HospitalId, e.CreatedAt });
        });

        modelBuilder.Entity<EhrVersion>(entity =>
        {
            entity.HasIndex(e => new { e.EhrId, e.Version }).IsUnique();
            entity.HasIndex(e => e.BlockchainTxHash).IsUnique();
            
            entity.Property(e => e.TxStatus)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<EhrFile>(entity =>
        {
            entity.HasIndex(e => new { e.EhrId, e.Version, e.ReportType });
            entity.HasIndex(e => new { e.CreatedBy, e.CreatedAt });
            
            entity.Property(e => e.ReportType)
                .HasConversion<string>()
                .HasMaxLength(30);
        });

        modelBuilder.Entity<EhrSubscription>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.Status });
            entity.HasIndex(e => new { e.EhrId, e.Status });
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
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
        });
    }
}
