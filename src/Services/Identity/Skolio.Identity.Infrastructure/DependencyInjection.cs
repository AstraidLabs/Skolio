using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Configuration;
using Skolio.Identity.Infrastructure.Persistence;
using Skolio.Identity.Infrastructure.Seeding;
using StackExchange.Redis;

namespace Skolio.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityDatabaseOptions>().Bind(configuration.GetSection(IdentityDatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<IdentityRedisOptions>().Bind(configuration.GetSection(IdentityRedisOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<Skolio.Identity.Infrastructure.Configuration.IdentityOptions>().Bind(configuration.GetSection(Skolio.Identity.Infrastructure.Configuration.IdentityOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OpenIddictOptions>().Bind(configuration.GetSection(OpenIddictOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<JwksOptions>().Bind(configuration.GetSection(JwksOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OpenIddictSigningOptions>().Bind(configuration.GetSection(OpenIddictSigningOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        var databaseOptions = configuration.GetSection(IdentityDatabaseOptions.SectionName).Get<IdentityDatabaseOptions>() ?? throw new InvalidOperationException("Missing IdentityDatabaseOptions configuration.");
        var redisOptions = configuration.GetSection(IdentityRedisOptions.SectionName).Get<IdentityRedisOptions>() ?? throw new InvalidOperationException("Missing IdentityRedisOptions configuration.");
        var oidcOptions = configuration.GetSection(OpenIddictOptions.SectionName).Get<OpenIddictOptions>() ?? throw new InvalidOperationException("Missing OpenIddictOptions configuration.");
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Missing JwtOptions configuration.");
        var signingOptions = configuration.GetSection(OpenIddictSigningOptions.SectionName).Get<OpenIddictSigningOptions>() ?? throw new InvalidOperationException("Missing OpenIddictSigningOptions configuration.");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName));
            options.UseOpenIddict();
        });

        services.AddIdentityCore<SkolioIdentityUser>(identityOptions =>
            {
                identityOptions.Password.RequiredLength = configuration.GetValue<int>("Identity:Identity:RequiredPasswordLength", 12);
                identityOptions.Password.RequireNonAlphanumeric = configuration.GetValue<bool>("Identity:Identity:RequireNonAlphanumeric", true);
                identityOptions.User.RequireUniqueEmail = true;
            })
            .AddRoles<SkolioIdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager<SignInManager<SkolioIdentityUser>>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "IdentitySmartScheme";
                options.DefaultChallengeScheme = "IdentitySmartScheme";
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
            })
            .AddPolicyScheme("IdentitySmartScheme", "Cookie or Bearer", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                    }

                    return IdentityConstants.ApplicationScheme;
                };
            });

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(oidcOptions.Issuer));
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");
                options.SetUserInfoEndpointUris("/connect/userinfo");
                options.SetEndSessionEndpointUris("/connect/logout");
                options.AllowAuthorizationCodeFlow();
                options.RequireProofKeyForCodeExchange();
                options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile, "skolio_api");
                options.DisableAccessTokenEncryption();
                options.SetAccessTokenLifetime(TimeSpan.Parse(jwtOptions.AccessTokenLifetime));
                if (jwtOptions.IssueRefreshTokens)
                {
                    options.AllowRefreshTokenFlow();
                }

                if (signingOptions.UseDevelopmentCertificate)
                {
                    options.AddDevelopmentSigningCertificate();
                    options.AddDevelopmentEncryptionCertificate();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(signingOptions.CertificatePath))
                    {
                        throw new InvalidOperationException("Signing certificate path must be configured in production mode.");
                    }

                    var certificate = new X509Certificate2(signingOptions.CertificatePath, signingOptions.CertificatePassword);
                    options.AddSigningCertificate(certificate);
                }

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddScoped<IIdentityCommandStore, IdentityCommandStore>();
        services.AddScoped<IIdentityReadStore, IdentityReadStore>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { redisOptions.ConnectionString },
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5000
        }));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });
        services.AddScoped<IdentityAuthSeeder>();

        return services;
    }
}
