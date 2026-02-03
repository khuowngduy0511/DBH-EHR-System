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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    
    // Swagger endpoints của các services (proxy qua gateway)
    c.SwaggerEndpoint("/api/auth/swagger/v1/swagger.json", "Auth Service API");
    c.SwaggerEndpoint("/api/ehr/swagger/v1/swagger.json", "EHR Service API");
    
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DBH-EHR API Gateway";
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
