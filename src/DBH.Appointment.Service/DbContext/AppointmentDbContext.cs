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

        // Appointment configuration
        modelBuilder.Entity<Models.Entities.Appointment>(entity =>
        {
            entity.HasMany(a => a.Encounters)
                .WithOne(e => e.Appointment)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.Status)
                .HasConversion<string>();

            entity.HasIndex(a => a.PatientId);
            entity.HasIndex(a => a.DoctorId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.ScheduledAt);
        });

        // Encounter configuration
        modelBuilder.Entity<Encounter>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.AppointmentId);
        });
    }
}
