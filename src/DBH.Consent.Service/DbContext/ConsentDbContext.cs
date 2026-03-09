using DBH.Consent.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Consent.Service.DbContext;

public class ConsentDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ConsentDbContext(DbContextOptions<ConsentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.Consent> Consents { get; set; } = null!;
    public DbSet<AccessRequest> AccessRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
