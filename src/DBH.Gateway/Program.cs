using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// YARP Reverse Proxy
// ============================================================================

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ============================================================================
// Authentication (JWT)
// ============================================================================

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
// Rate Limiting
// ============================================================================

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ============================================================================
// Swagger
// ============================================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DBH-EHR System API Gateway",
        Version = "v1",
        Description = "API Gateway cho hệ thống DBH-EHR - Decentralized Blockchain Healthcare System"
    });
});

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
    
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://dbh-ehr.vn",
                "https://admin.dbh-ehr.vn",
                "https://app.dbh-ehr.vn")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ============================================================================
// Middleware Pipeline
// ============================================================================

// Rate Limiting
app.UseIpRateLimiting();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Downstream services swagger endpoints
    c.SwaggerEndpoint("/services/auth/swagger/v1/swagger.json", "Auth Service API v1");
    c.SwaggerEndpoint("/services/organization/swagger/v1/swagger.json", "Organization Service API v1");
    c.SwaggerEndpoint("/services/ehr/swagger/v1/swagger.json", "EHR Service API v1");
    c.SwaggerEndpoint("/services/consent/swagger/v1/swagger.json", "Consent Service API v1");
    c.SwaggerEndpoint("/services/audit/swagger/v1/swagger.json", "Audit Service API v1");
    c.SwaggerEndpoint("/services/notification/swagger/v1/swagger.json", "Notification Service API v1");
    
    // Gateway's own swagger
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DBH-EHR System APIs";
    c.DisplayRequestDuration();
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    Status = "healthy", 
    Service = "DBH.Gateway",
    Timestamp = DateTime.UtcNow 
}));

// Gateway info endpoint
app.MapGet("/", () => Results.Ok(new
{
    Name = "DBH-EHR API Gateway",
    Version = "1.0.0",
    Description = "Decentralized Blockchain Healthcare System for Secure EHR Management",
    Endpoints = new
    {
        Swagger = "/swagger",
        Health = "/health",
        Auth = "/api/v1/auth/*",
        Did = "/api/v1/did/*",
        Organizations = "/api/v1/organizations/*",
        Ehr = "/api/v1/ehr/*",
        Fhir = "/api/v1/fhir/*",
        Consents = "/api/v1/consents/*",
        Audit = "/api/v1/audit/*",
        Notifications = "/api/v1/notifications/*"
    }
}));

// YARP Reverse Proxy
app.MapReverseProxy();

await app.RunAsync();
