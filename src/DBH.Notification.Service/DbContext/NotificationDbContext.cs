using DBH.Notification.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.Notification.Service.DbContext;

public class NotificationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationEntity> Notifications { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Notification
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasIndex(e => e.RecipientDid);
            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // Store enums as strings
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Channel).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // DeviceToken
        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasIndex(e => e.UserDid);
            entity.HasIndex(e => e.FcmToken);
            entity.HasIndex(e => new { e.UserDid, e.IsActive });
        });

        // NotificationPreference
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasIndex(e => e.UserDid).IsUnique();
        });
    }
}
