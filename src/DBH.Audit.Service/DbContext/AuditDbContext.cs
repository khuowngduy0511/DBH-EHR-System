using DBH.Audit.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Audit.Service.DbContext;

public class AuditDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
