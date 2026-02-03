var builder = WebApplication.CreateBuilder(args);

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DBH-EHR System API Gateway",
        Version = "v1",
        Description = "API Gateway cho hệ thống DBH-EHR"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Swagger endpoints của các downstream services (proxy qua gateway)
    c.SwaggerEndpoint("/services/ehr/swagger/v1/swagger.json", "DBH EHR Service API v1");
    // c.SwaggerEndpoint("/services/auth/swagger/v1/swagger.json", "DBH Auth Service API v1");
    
    // Gateway's own swagger
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DBH-EHR System APIs";
    c.DisplayRequestDuration();
});

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    Status = "Healthy", 
    Service = "DBH.Gateway",
    Timestamp = DateTime.UtcNow 
}));

// Gateway info endpoint
app.MapGet("/", () => Results.Ok(new
{
    Name = "DBH-EHR API Gateway",
    Version = "1.0.0",
    Endpoints = new
    {
        Swagger = "/swagger",
        Health = "/health",
        Auth = "/api/auth/*",
        Ehr = "/api/ehr/*"
    }
}));

// YARP Reverse Proxy
app.MapReverseProxy();

app.Run();
