using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Config;
using TowerFight.BusinessLogic.Services;
using Microsoft.EntityFrameworkCore;
using TowerFight.API.Utilities;

namespace TowerFight.API.Bootstrap;

public static class InternalServicesExtensions
{
    public static IServiceCollection AddInternalServices(this IServiceCollection services)
    {
        services.AddTransient<ILeadersService, LeadersService>();
        services.AddTransient<ICacheService, CacheService>();
        services.AddSingleton<HighscoreHashUtility>();
        return services;
    }

    public static IServiceCollection AddDbServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        const string assemblyName = $"{nameof(TowerFight)}.{nameof(API)}";

        var dbSettings = configuration.GetRequiredSection(nameof(DbSettings)).Get<DbSettings>()!;

        services.AddDbContextFactory<AppDbContext>(GetOptions);
        
        return services;

        void GetOptions(DbContextOptionsBuilder options) => 
            options.UseNpgsql(dbSettings.PgConnectionString, b => b.MigrationsAssembly(assemblyName));
    }
}
