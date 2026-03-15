using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Infrastructure.Auth;
using Skolio.Academics.Infrastructure.Configuration;
using Skolio.Academics.Infrastructure.Persistence;
using Skolio.Academics.Infrastructure.Services;
using Skolio.Academics.Infrastructure.Seeding;
using StackExchange.Redis;

namespace Skolio.Academics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAcademicsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AcademicsDatabaseOptions>().Bind(configuration.GetSection(AcademicsDatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AcademicsRedisOptions>().Bind(configuration.GetSection(AcademicsRedisOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        var databaseOptions = configuration.GetSection(AcademicsDatabaseOptions.SectionName).Get<AcademicsDatabaseOptions>() ?? throw new InvalidOperationException("Missing AcademicsDatabaseOptions configuration.");
        var redisOptions = configuration.GetSection(AcademicsRedisOptions.SectionName).Get<AcademicsRedisOptions>() ?? throw new InvalidOperationException("Missing AcademicsRedisOptions configuration.");

        services.AddDbContext<AcademicsDbContext>(options => options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName)));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, ClaimsCurrentUserContext>();
        services.AddScoped<IAcademicsCommandStore, AcademicsCommandStore>();
        services.AddSingleton<IAcademicsClock, SystemAcademicsClock>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { redisOptions.ConnectionString },
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5000
        }));
        services.AddStackExchangeRedisCache(options => { options.Configuration = redisOptions.ConnectionString; options.InstanceName = redisOptions.InstanceName; });
        services.AddScoped<AcademicsReferenceSeeder>();
        return services;
    }
}
