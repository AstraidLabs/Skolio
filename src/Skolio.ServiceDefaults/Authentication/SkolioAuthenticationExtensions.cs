using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.ServiceDefaults.Authentication;

public static class SkolioAuthenticationExtensions
{
    public static IServiceCollection AddSkolioJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName)
    {
        services.AddOptions<JwtValidationOptions>()
            .Bind(configuration.GetSection(configSectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(configSectionName).Get<JwtValidationOptions>()
            ?? throw new InvalidOperationException($"Missing auth configuration in section '{configSectionName}'.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = jwtOptions.Authority;
                options.Audience = jwtOptions.Audience;
                options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                options.TokenValidationParameters.RoleClaimType = "role";
            });

        return services;
    }
}
