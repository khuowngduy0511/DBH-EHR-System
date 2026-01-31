using DBH.EHR.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Data;

/// <summary>
/// PostgreSQL Replica DbContex
/// </summary>
public class EhrReplicaDbContext : DbContext
{
    public EhrReplicaDbContext(DbContextOptions<EhrReplicaDbContext> options) 
        : base(options)
    {
    }

    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<EhrIndex> EhrIndex => Set<EhrIndex>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<EhrIndex>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.OwnerOrg);
            entity.HasIndex(e => e.OffchainDocId);
            entity.HasIndex(e => e.Status);
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });
    }
}