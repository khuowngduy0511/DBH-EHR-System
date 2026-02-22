using System.Text.Json.Serialization;
using DBH.Auth.Service.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Configuration
// ============================================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "DBH Auth Service API", 
        Version = "v1",
        Description = "Authentication & Authorization Service cho hệ thống DBH-EHR"
    });
});

// ============================================================================
// Database Configuration (Auth Service own database)
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("AuthDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
    });
});

var app = builder.Build();

// ============================================================================
// Database Initialization
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Auth database...");
        await authDb.Database.MigrateAsync();
        logger.LogInformation("Auth database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
        await authDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Auth Service API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "healthy", 
    Service = "DBH.Auth.Service",
    Timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();

