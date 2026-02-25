using System.Text;
using System.Text.Json.Serialization;
using DBH.Notification.Service.Data;
using DBH.Notification.Service.Services;
using DBH.Notification.Service.Models.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        Title = "DBH Notification Service API", 
        Version = "v1",
        Description = "Notification Service cho hệ thống DBH-EHR"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Example: 'Bearer {token}'",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================================
// JWT Authentication
// ============================================================================

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ============================================================================
// Service Registration
// ============================================================================

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Firebase configuration
builder.Services.Configure<FirebaseConfig>(builder.Configuration.GetSection("Firebase"));

// ============================================================================
// Database Configuration (Notification Service own database)
// ============================================================================

var connectionString = builder.Configuration.GetConnectionString("NotificationDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<NotificationDbContext>(options =>
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

// TODO: Add Firebase Admin SDK initialization
// TODO: Add RabbitMQ/Azure Service Bus consumer

var app = builder.Build();

// ============================================================================
// Database Initialization
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var notificationDb = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying PostgreSQL migrations to Notification database...");
        await notificationDb.Database.MigrateAsync();
        logger.LogInformation("Notification database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
        await notificationDb.Database.EnsureCreatedAsync();
    }
}

// ============================================================================
// HTTP Pipeline
// ============================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBH Notification Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "healthy", 
    Service = "DBH.Notification.Service",
    Timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

await app.RunAsync();
