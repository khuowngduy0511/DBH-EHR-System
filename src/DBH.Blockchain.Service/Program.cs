using System.Text.Json.Serialization;
using DBH.Blockchain.Service.Controllers;
using DBH.Shared.Infrastructure;
using DBH.Shared.Infrastructure.Authentication;
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
        Title = "DBH Blockchain API",
        Version = "v1",
        Description = "Blockchain API for error logging, emergency access, and account management"
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

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("AuthService", client =>
{
    var authUrl = builder.Configuration["ServiceUrls:AuthService"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(authUrl);
});

builder.Services.AddHttpClient("EhrService", client =>
{
    var ehrUrl = builder.Configuration["ServiceUrls:EhrService"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(ehrUrl);
});

// ============================================================================
// Authentication & Authorization
// ============================================================================

builder.Services
    .AddDbhAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

// ============================================================================
// Hyperledger Fabric & Blockchain Services
// ============================================================================

builder.Services.AddHyperledgerFabric(builder.Configuration.GetSection("HyperledgerFabric"));

// ============================================================================
// CORS
// ============================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ============================================================================
// Build App
// ============================================================================

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blockchain API v1");
});

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "healthy",
    Service = "DBH.Blockchain.Service",
    Timestamp = DateTime.UtcNow
}));

// ============================================================================
// Logging Configuration
// ============================================================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Blockchain API service...");

app.Run();
