using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Infrastructure.Configuration;
using Skolio.Organization.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Skolio.Organization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OrganizationDatabaseOptions>()
            .Bind(configuration.GetSection(OrganizationDatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<OrganizationRedisOptions>()
            .Bind(configuration.GetSection(OrganizationRedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var databaseOptions = configuration.GetSection(OrganizationDatabaseOptions.SectionName).Get<OrganizationDatabaseOptions>()
            ?? throw new InvalidOperationException("Missing OrganizationDatabaseOptions configuration.");

        var redisOptions = configuration.GetSection(OrganizationRedisOptions.SectionName).Get<OrganizationRedisOptions>()
            ?? throw new InvalidOperationException("Missing OrganizationRedisOptions configuration.");

        services.AddDbContext<OrganizationDbContext>(options =>
            options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName)));

        services.AddScoped<IOrganizationCommandStore, OrganizationCommandStore>();
        services.AddScoped<IOrganizationReadStore, OrganizationReadStore>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        return services;
    }
}
