using System.Text.Json.Serialization;
using DBH.Audit.Service.DbContext;
using DBH.Audit.Service.Services;
using DBH.Shared.Infrastructure;
using DBH.Shared.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
        Title = "DBH Audit Service API", 
        Version = "v1",
        Description = "Audit Logging Service (Blockchain) cho hệ thống DBH-EHR"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================================
// Database Configuration (Audit Service own database)
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("AuditDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AuditDbContext>(options =>
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

// ============================================================================
// Service Registration
// ============================================================================
builder.Services.AddScoped<IAuditService, AuditService>();

// ============================================================================
// Hyperledger Fabric Blockchain Integration
// ============================================================================
builder.Services.AddHyperledgerFabric(builder.Configuration);

// ============================================================================
// JWT Authentication
// ============================================================================

builder.Services.AddDbhAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================================================
// Database Initialization
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Audit database...");
        await auditDb.Database.MigrateAsync();
        logger.LogInformation("Audit database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
        await auditDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Audit Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "healthy", 
    Service = "DBH.Audit.Service",
    Timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
