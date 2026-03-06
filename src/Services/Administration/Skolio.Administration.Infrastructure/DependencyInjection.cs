using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.Administration.Infrastructure.Configuration;
using Skolio.Administration.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Skolio.Administration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AdministrationDatabaseOptions>()
            .Bind(configuration.GetSection(AdministrationDatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AdministrationRedisOptions>()
            .Bind(configuration.GetSection(AdministrationRedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AdministrationHangfireOptions>()
            .Bind(configuration.GetSection(AdministrationHangfireOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        var databaseOptions = configuration.GetSection(AdministrationDatabaseOptions.SectionName).Get<AdministrationDatabaseOptions>()
            ?? throw new InvalidOperationException("Missing AdministrationDatabaseOptions configuration.");

        var redisOptions = configuration.GetSection(AdministrationRedisOptions.SectionName).Get<AdministrationRedisOptions>()
            ?? throw new InvalidOperationException("Missing AdministrationRedisOptions configuration.");

        services.AddDbContext<AdministrationDbContext>(options =>
        {
            options.UseNpgsql(databaseOptions.ConnectionString);
        });

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        services.AddHangfire((sp, config) =>
        {
            var hangfireOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdministrationHangfireOptions>>().Value;
            config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireOptions.StorageConnectionString));
        });

        return services;
    }
}
