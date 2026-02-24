using DBH.Shared.Infrastructure.Caching;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Storage;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBH.Shared.Infrastructure;

/// <summary>
/// Extension methods để đăng ký Infrastructure Services
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Đăng ký tất cả Infrastructure Services
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<InfrastructureOptions>? configureOptions = null)
    {
        var options = new InfrastructureOptions();
        configureOptions?.Invoke(options);

        if (options.UseS3Storage)
        {
            services.AddS3Storage(configuration);
        }

        if (options.UseRedisCache)
        {
            services.AddRedisCache(configuration);
        }

        if (options.UseRabbitMQ)
        {
            services.AddRabbitMQ(configuration, options.ConfigureConsumers);
        }

        return services;
    }

    /// <summary>
    /// Đăng ký S3 Storage Service
    /// </summary>
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3StorageOptions>(options =>
        {
            var section = configuration.GetSection(S3StorageOptions.SectionName);
            section.Bind(options);
        });
        services.AddSingleton<IS3StorageService, S3StorageService>();

        return services;
    }

    /// <summary>
    /// Đăng ký Redis Cache Service
    /// </summary>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSection = configuration.GetSection(RedisCacheOptions.SectionName);
        var redisOptions = new RedisCacheOptions();
        redisSection.Bind(redisOptions);

        if (redisOptions.Enabled)
        {
            services.Configure<RedisCacheOptions>(options => redisSection.Bind(options));
            services.AddSingleton<ICacheService, RedisCacheService>();

            // Also register StackExchange.Redis IDistributedCache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Đăng ký RabbitMQ Message Queue với MassTransit
    /// </summary>
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitSection = configuration.GetSection(RabbitMQOptions.SectionName);
        var rabbitOptions = new RabbitMQOptions();
        rabbitSection.Bind(rabbitOptions);

        services.Configure<RabbitMQOptions>(options => rabbitSection.Bind(options));

        services.AddMassTransit(x =>
        {
            // Register consumers
            configureConsumers?.Invoke(x);

            if (rabbitOptions.Enabled)
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitOptions.Host, (ushort)rabbitOptions.Port, rabbitOptions.VirtualHost, h =>
                    {
                        h.Username(rabbitOptions.Username);
                        h.Password(rabbitOptions.Password);

                        if (rabbitOptions.UseSsl)
                        {
                            h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                        }
                    });

                    cfg.PrefetchCount = rabbitOptions.PrefetchCount;

                    // Configure retry policy
                    cfg.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(rabbitOptions.RetryIntervalSeconds),
                        TimeSpan.FromSeconds(rabbitOptions.RetryIntervalSeconds * 2),
                        TimeSpan.FromSeconds(rabbitOptions.RetryIntervalSeconds * 4)));

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Use in-memory transport for development
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        services.AddScoped<IMessagePublisher, MassTransitMessagePublisher>();

        return services;
    }
}

/// <summary>
/// Options cho Infrastructure configuration
/// </summary>
public class InfrastructureOptions
{
    public bool UseS3Storage { get; set; } = true;
    public bool UseRedisCache { get; set; } = true;
    public bool UseRabbitMQ { get; set; } = true;
    public Action<IBusRegistrationConfigurator>? ConfigureConsumers { get; set; }
}
