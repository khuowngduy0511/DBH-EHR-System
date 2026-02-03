using System.Text.Json.Serialization;
using DBH.EHR.Service.Data;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.EHR.Service.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

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
        Description = "EHR Service cho hệ thống DBH-EHR"
    });
});

// ============================================================================
// Database Configuration
// ============================================================================

// PostgreSQL Primary (Read-Write)
builder.Services.AddDbContext<EhrPrimaryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresPrimary"));
});

// PostgreSQL Replica (Read-Only)
builder.Services.AddDbContext<EhrReplicaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresReplica"));
});

// MongoDB Client (singleton - manages connection pool internally)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration["MongoDB:ConnectionString"];
    return new MongoClient(connectionString);
});

// MongoDB Context
builder.Services.AddSingleton<MongoDbContext>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "dbh_ehr";
    return new MongoDbContext(client, databaseName);
});

// ============================================================================
// Repositories
// ============================================================================

builder.Services.AddScoped<IEhrRecordRepository, EhrRecordRepository>();
builder.Services.AddScoped<IEhrDocumentRepository, EhrDocumentRepository>();

// ============================================================================
// Services
// ============================================================================

builder.Services.AddScoped<IEhrService, EhrService>();

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
