
    using System.Text;
    using DBH.Auth.Service.DbContext;
    using DBH.Auth.Service.Repositories;
    using DBH.Auth.Service.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using DBH.Shared.Contracts.Blockchain;
    using DBH.Shared.Infrastructure;
    using DBH.Shared.Infrastructure.Blockchain;

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();

    // Swagger Configuration with JWT Support
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "DBH.Auth.Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Bearer token",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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


    // Database Configuration
    var pgConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseNpgsql(pgConnectionString)
        .EnableSensitiveDataLogging() // Shows the actual values being sent
       .EnableDetailedErrors());      // Provides more stack trace detail

    // Repositories
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // Auth Services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // HTTP Client for Organization Service
    builder.Services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>()
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

    // Fabric CA enrollment (singleton because it caches admin crypto material)
    builder.Services.Configure<FabricCaOptions>(builder.Configuration.GetSection(FabricCaOptions.SectionName));
    builder.Services.AddSingleton<IFabricCaService, FabricCaService>();
    builder.Services.AddHyperledgerFabric(builder.Configuration);

    // JWT Authentication Configuration
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["Key"];

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/health", () => Results.Ok("Auth Service is healthy"));

    using (var scope = app.Services.CreateScope())
    {
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Applying PostgreSQL migrations to Auth database...");
            await authDb.Database.MigrateAsync();
            logger.LogInformation("Auth database migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration failed. Ensuring tables exist...");
            await authDb.Database.EnsureCreatedAsync();
        }
    }
    app.Run();

