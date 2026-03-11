using TowerFight.API.Health;
using TowerFight.BusinessLogic.Data.Config;
using TowerFight.BusinessLogic.Data.Redis;
using TowerFight.BusinessLogic.Data.RedisDI;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TowerFight.API.Bootstrap;

public static class InfrastructureExtensions
{
    public static ConfigurationManager SetupConfiguration(this ConfigurationManager manager)
    {
        manager.AddJsonFile("secrets.json", true, true);
        return manager;
    }

    public static IServiceCollection AddConfigOptionsAndBind<TOptions>(this IServiceCollection services,
        IConfiguration configuration, string section, out TOptions instance) where TOptions : class, new()
    {
        var configSection = configuration.GetSection(section);

        var options = new TOptions();
        configSection.Bind(options);
        instance = options;

        services.Configure<TOptions>(configSection);
        return services;
    }

    public static WebApplicationBuilder AddConfigOptions<TOptions>(this WebApplicationBuilder builder) where TOptions : class, new()
    {
        var configSection = builder.Configuration.GetSection(typeof(TOptions).Name);
        
        builder.Services.Configure<TOptions>(configSection);
        return builder;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection serviceCollection)
    {
        const string version = "v1";
        const string scheme = "basic";
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version,
                new OpenApiInfo
                {
                    Title = "TowerFight API Documentation",
                    Version = version
                });
            options.IncludeXmlComments(Path.ChangeExtension(typeof(Program).Assembly.Location, "xml"));
            options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Scheme = scheme
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = scheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
            options.DocumentFilter<HealthCheckEndpointFilter>();
        });

        return serviceCollection;
    }
    
    public static IServiceCollection AddRedis(this IServiceCollection serviceCollection, RedisSettings redisSettings)
    {
        serviceCollection.RegisterRedisServices(redisSettings.UseInMemoryCache);

        return serviceCollection;
    }

    public static void StartRedis(this IApplicationBuilder applicationBuilder, bool useInMemoryCache)
    {
        if (!useInMemoryCache)
            applicationBuilder.ApplicationServices.GetRequiredService<IRedisConnectionFactory>().Start(); 
    }

    public static IServiceCollection AddHealth(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddHealthChecks()
            .AddCheck<ApiHealthCheck>("Api")
            .AddNpgSql(configuration[$"{nameof(DbSettings)}:{nameof(DbSettings.PgConnectionString)}"]!,
                failureStatus: HealthStatus.Degraded);

        return serviceCollection;
    }
}