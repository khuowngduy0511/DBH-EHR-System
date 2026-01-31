var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read database connection strings from environment variables (Docker-friendly)
var pgConnectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_POSTGRESQL")
    ?? "Host=pg_primary;Port=5432;Database=dbh_ehr;Username=dbh_admin;Password=dbh_secret_2024";

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_MONGODB")
    ?? "mongodb://mongo1:27017,mongo2:27017/?replicaSet=rs0";

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint (required for Docker health checks)
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    service = "DBH.Auth.Service",
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithTags("Health");

// Database configuration endpoint (for demo/debugging)
app.MapGet("/config/databases", () => Results.Ok(new {
    postgresql = new {
        connectionConfigured = !string.IsNullOrEmpty(pgConnectionString),
        host = "pg_primary (via Docker network)"
    },
    mongodb = new {
        connectionConfigured = !string.IsNullOrEmpty(mongoConnectionString),
        replicaSet = "rs0 (mongo1, mongo2)"
    }
}))
.WithName("GetDatabaseConfig")
.WithTags("Configuration");

// Original weather forecast endpoint (keeping for backward compatibility)
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithTags("Sample")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

