using DBH.Appointment.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Appointment.Service.DbContext;

public class AppointmentDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.Appointment> Appointments { get; set; } = null!;
    public DbSet<Encounter> Encounters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Additional configurations if needed
        modelBuilder.Entity<Models.Entities.Appointment>()
            .HasMany(a => a.Encounters)
            .WithOne(e => e.Appointment)
            .HasForeignKey(e => e.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
