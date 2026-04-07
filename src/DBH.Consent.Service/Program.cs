using System.Text.Json.Serialization;
using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.Services;
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
        Title = "DBH Consent Service API", 
        Version = "v1",
        Description = "Consent Management Service (Blockchain) cho hệ thống DBH-EHR"
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
// Database Configuration (Consent Service own database)
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("ConsentDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ConsentDbContext>(options =>
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
// Application Services
// ============================================================================

builder.Services.AddScoped<IConsentService, ConsentService>();
builder.Services.AddHttpContextAccessor();

// ============================================================================
// HTTP Clients for inter-service calls
// ============================================================================
builder.Services.AddHttpClient("AuthService", client =>
{
    var authUrl = builder.Configuration["ServiceUrls:AuthService"] ?? "http://localhost:5101";
    client.BaseAddress = new Uri(authUrl);
});

builder.Services.AddHttpClient("EhrService", client =>
{
    var ehrUrl = builder.Configuration["ServiceUrls:EhrService"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(ehrUrl);
});

// ============================================================================
// Blockchain Services (Hyperledger Fabric)
// ============================================================================
builder.Services.AddHyperledgerFabric(builder.Configuration);

// ============================================================================
// Notification Client
// ============================================================================
builder.Services.AddNotificationClient(builder.Configuration);

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
    var consentDb = scope.ServiceProvider.GetRequiredService<ConsentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Consent database...");
        await consentDb.Database.MigrateAsync();
        logger.LogInformation("Consent database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
        await consentDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Consent Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "healthy", 
    Service = "DBH.Consent.Service",
    Timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
