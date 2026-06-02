using DBH.Auth.Service.DbContext;
using DBH.Auth.Service.Models.Entities;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBH.Auth.Service.Services;

/// <summary>
/// On startup, enrolls seed users (created via EF Core HasData) with the Fabric CA.
/// Seed users are inserted directly into the database and never go through the
/// normal registration flow which enqueues CA enrollment. This service bridges that gap.
/// </summary>
public class SeedUserCaEnrollmentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeedUserCaEnrollmentService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public SeedUserCaEnrollmentService(
        IServiceScopeFactory scopeFactory,
        ILogger<SeedUserCaEnrollmentService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay to allow services (DB, Fabric CA, etc.) to be fully ready
        const int startupDelaySeconds = 15;
        _logger.LogInformation(
            "SeedUserCaEnrollmentService starting. Will wait {Delay}s before enrolling seed users...",
            startupDelaySeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(startupDelaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await EnrollSeedUsersAsync(stoppingToken);
    }

    private async Task EnrollSeedUsersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var fabricCaService = scope.ServiceProvider.GetRequiredService<IFabricCaService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedUserCaEnrollmentService>>();

        // Find all active seed users who have NOT yet been enrolled with the CA
        var unenrolledUsers = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.CaEnrolledAt == null)
            .ToListAsync(ct);

        if (unenrolledUsers.Count == 0)
        {
            logger.LogInformation("No unenrolled seed users found. All users already registered with Fabric CA.");
            return;
        }

        logger.LogInformation(
            "Found {Count} unenrolled seed users. Attempting Fabric CA enrollment...",
            unenrolledUsers.Count);

        var enrolledCount = 0;
        var failedCount = 0;

        foreach (var user in unenrolledUsers)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Determine role name from the user's roles
                var primaryRole = user.UserRoles
                    .Select(ur => ur.Role?.RoleName)
                    .FirstOrDefault(r => r != null);

                var roleName = primaryRole?.ToString() ?? "Patient";
                var username = user.FullName ?? user.Email ?? user.UserId.ToString();
                var enrollmentId = user.UserId.ToString();

                logger.LogInformation(
                    "Enrolling seed user {UserId} ({Username}) with role {Role}, org {OrgId}...",
                    enrollmentId, username, roleName, user.OrganizationId);

                var result = await fabricCaService.EnrollUserAsync(
                    enrollmentId,
                    username,
                    roleName,
                    organizationId: user.OrganizationId);

                if (result.Success)
                {
                    user.CaEnrolledAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(ct);
                    enrolledCount++;

                    logger.LogInformation(
                        "Successfully enrolled seed user {UserId} with Fabric CA. Certificate stored at {Path}",
                        enrollmentId, result.AccountStoragePath);
                }
                else
                {
                    failedCount++;
                    logger.LogWarning(
                        "Fabric CA enrollment returned non-success for seed user {UserId}: {Error}",
                        enrollmentId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogWarning(
                    ex,
                    "Failed to enroll seed user {UserId} with Fabric CA. Will retry on next startup.",
                    user.UserId);
            }
        }

        logger.LogInformation(
            "Seed user CA enrollment complete. Enrolled: {Enrolled}, Failed: {Failed}",
            enrolledCount, failedCount);
    }
}