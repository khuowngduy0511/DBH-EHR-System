using System.Text.Json.Serialization;
using DBH.Payment.Service.DbContext;
using DBH.Payment.Service.Services;
using DBH.Shared.Contracts;
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
        Title = "DBH Payment Service API",
        Version = "v1",
        Description = "Payment & Invoice Service cho hệ thống DBH-EHR"
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
// Database Configuration
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PaymentDbContext>(options =>
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
// Infrastructure (RabbitMQ)
// ============================================================================

builder.Services.AddInfrastructure(builder.Configuration, options =>
{
    options.UseRabbitMQ = true;
    options.UseS3Storage = false;
    options.UseRedisCache = false;
    options.UseHyperledgerFabric = false;
    options.UseNotificationClient = false;
});

// ============================================================================
// HTTP Clients (Service-to-Service)
// ============================================================================

builder.Services.AddHttpClient("OrganizationService", client =>
{
    var orgServiceUrl = builder.Configuration["ServiceUrls:OrganizationService"] ?? "http://organization_service:5002";
    client.BaseAddress = new Uri(orgServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ============================================================================
// Application Services
// ============================================================================

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();

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
    var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Payment database...");
        await paymentDb.Database.MigrateAsync();
        logger.LogInformation("Payment database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
        await paymentDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Payment Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "healthy",
    Service = "DBH.Payment.Service",
    Timestamp = VietnamTimeHelper.Now
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
