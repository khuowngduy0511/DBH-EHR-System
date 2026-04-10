using DBH.Organization.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Organization.Service.DbContext;

public class OrganizationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.Organization> Organizations { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Membership> Memberships { get; set; } = null!;
    public DbSet<PaymentConfig> PaymentConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>()
            .HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Membership>()
            .HasOne(m => m.Organization)
            .WithMany(o => o.Memberships)
            .HasForeignKey(m => m.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Membership>()
            .HasOne(m => m.Department)
            .WithMany(d => d.Memberships)
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Organization)
            .WithMany(o => o.Departments)
            .HasForeignKey(d => d.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentConfig>()
            .HasOne(pc => pc.Organization)
            .WithOne(o => o.PaymentConfig)
            .HasForeignKey<PaymentConfig>(pc => pc.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentConfig>()
            .HasIndex(pc => pc.OrgId)
            .IsUnique();
    }
}
