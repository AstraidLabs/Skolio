using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.Communication.Infrastructure.Configuration;
using Skolio.Communication.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Skolio.Communication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CommunicationDatabaseOptions>()
            .Bind(configuration.GetSection(CommunicationDatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<CommunicationRedisOptions>()
            .Bind(configuration.GetSection(CommunicationRedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        var databaseOptions = configuration.GetSection(CommunicationDatabaseOptions.SectionName).Get<CommunicationDatabaseOptions>()
            ?? throw new InvalidOperationException("Missing CommunicationDatabaseOptions configuration.");

        var redisOptions = configuration.GetSection(CommunicationRedisOptions.SectionName).Get<CommunicationRedisOptions>()
            ?? throw new InvalidOperationException("Missing CommunicationRedisOptions configuration.");

        services.AddDbContext<CommunicationDbContext>(options =>
        {
            options.UseNpgsql(databaseOptions.ConnectionString);
        });

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        return services;
    }
}
