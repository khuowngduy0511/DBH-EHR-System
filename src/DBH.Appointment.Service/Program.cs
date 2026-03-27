using System.Text.Json.Serialization;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.Services;
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
        Title = "DBH Appointment Service API", 
        Version = "v1",
        Description = "Appointment and Encounter Management Service for DBH-EHR System"
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

// Configure standard JWT Auth from DBH.Shared.Infrastructure if present
builder.Services.AddDbhAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// ============================================================================
// Database Configuration
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("AppointmentDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppointmentDbContext>(options =>
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
// Infrastructure — RabbitMQ / MassTransit
// ============================================================================

builder.Services.AddInfrastructure(builder.Configuration, options =>
{
    options.UseRabbitMQ = true;
    options.UseNotificationClient = true;
});

// ============================================================================
// Application Services
// ============================================================================

builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddHttpContextAccessor();

// ============================================================================
// HttpClient Factory — inter-service communication
// ============================================================================

var serviceUrls = builder.Configuration.GetSection("ServiceUrls");

builder.Services.AddHttpClient("EhrService", client =>
{
    client.BaseAddress = new Uri(serviceUrls["EhrService"] ?? "http://ehr_service:5003");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(serviceUrls["OrganizationService"] ?? "http://organization_service:5002");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("ConsentService", client =>
{
    client.BaseAddress = new Uri(serviceUrls["ConsentService"] ?? "http://consent_service:5004");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(serviceUrls["AuthService"] ?? "http://auth_service:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// ============================================================================
// Database Initialization
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var apptDb = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Appointment database...");
        await apptDb.Database.MigrateAsync();
        logger.LogInformation("Appointment database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist dynamically...");
        await apptDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Appointment Service API v1");
    c.RoutePrefix = "swagger"; // Serves at /swagger
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "healthy", 
    Service = "DBH.Appointment.Service",
    Timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
