using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.Identity.Infrastructure.Configuration;
using Skolio.Identity.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Skolio.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<IdentityDatabaseOptions>()
            .Bind(configuration.GetSection(IdentityDatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<IdentityRedisOptions>()
            .Bind(configuration.GetSection(IdentityRedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<OpenIddictOptions>()
            .Bind(configuration.GetSection(OpenIddictOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<JwksOptions>()
            .Bind(configuration.GetSection(JwksOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        var databaseOptions = configuration.GetSection(IdentityDatabaseOptions.SectionName).Get<IdentityDatabaseOptions>()
            ?? throw new InvalidOperationException("Missing IdentityDatabaseOptions configuration.");

        var redisOptions = configuration.GetSection(IdentityRedisOptions.SectionName).Get<IdentityRedisOptions>()
            ?? throw new InvalidOperationException("Missing IdentityRedisOptions configuration.");

        services.AddDbContext<IdentityDbContext>(options =>
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
