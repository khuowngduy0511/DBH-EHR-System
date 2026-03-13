using System.Text.Json.Serialization;
using DBH.EHR.Service.DbContext;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.EHR.Service.Services;
using DBH.Shared.Infrastructure;
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
        Title = "DBH EHR Service API", 
        Version = "v1",
        Description = "EHR Service cho hệ thống DBH-EHR - Decentralized Blockchain Healthcare"
    });
});

// ============================================================================
// Database Configuration (EHR Service own databases)
// ============================================================================

// PostgreSQL Primary (Read-Write)
var primaryConnectionString = builder.Configuration.GetConnectionString("EhrDb")
    ?? builder.Configuration.GetConnectionString("PostgresPrimary");

builder.Services.AddDbContext<EhrPrimaryDbContext>(options =>
{
    options.UseNpgsql(primaryConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
    });
});

// PostgreSQL Replica (Read-Only) - Falls back to primary if not configured
var replicaConnectionString = builder.Configuration.GetConnectionString("PostgresReplica");
var effectiveReplicaConnectionString = string.IsNullOrEmpty(replicaConnectionString)
    ? primaryConnectionString
    : replicaConnectionString;

builder.Services.AddDbContext<EhrReplicaDbContext>(options =>
{
    options.UseNpgsql(effectiveReplicaConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
    });
});

// MongoDB FHIR Context
builder.Services.Configure<MongoDbConfiguration>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// ============================================================================
// Repositories
// ============================================================================

builder.Services.AddScoped<IEhrRecordRepository, EhrRecordRepository>();
builder.Services.AddScoped<IEhrDocumentRepository, EhrDocumentRepository>();

// ============================================================================
// Services
// ============================================================================

builder.Services.AddScoped<IEhrService, EhrService>();

// ============================================================================
// Blockchain Services (Hyperledger Fabric)
// ============================================================================
builder.Services.AddHyperledgerFabric(builder.Configuration);

// ============================================================================
// HTTP Client for inter-service calls (Consent verification)
// ============================================================================
builder.Services.AddHttpClient("ConsentService", client =>
{
    var consentUrl = builder.Configuration["ServiceUrls:ConsentService"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(consentUrl);
});

builder.Services.AddHttpClient("AuthService", client =>
{
    var authUrl = builder.Configuration["ServiceUrls:AuthService"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(authUrl);
});

var app = builder.Build();

// ============================================================================
// Database Initialization
// ============================================================================

// Auto-migrate PostgreSQL Primary (creates tables if not exist)
using (var scope = app.Services.CreateScope())
{
    var primaryDb = scope.ServiceProvider.GetRequiredService<EhrPrimaryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to primary...");
        await primaryDb.Database.MigrateAsync();
        logger.LogInformation("PostgreSQL migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed (tables may already exist via init script). Ensuring tables exist...");
        await primaryDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH EHR Service API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
});

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "healthy",
    Service = "DBH.EHR.Service",
    Timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
