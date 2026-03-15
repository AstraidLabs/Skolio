using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skolio.ServiceDefaults.Authentication;
using Skolio.ServiceDefaults.Authorization;
using Skolio.ServiceDefaults.Middleware;

namespace Skolio.ServiceDefaults.Extensions;

public static class SkolioServiceDefaultsExtensions
{
    public static IServiceCollection AddSkolioServiceDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        string authConfigSection,
        Action<AuthorizationOptions>? configureAdditionalPolicies = null,
        Action<CorsPolicyBuilder>? configureCors = null)
    {
        services.AddSkolioJwtAuthentication(configuration, authConfigSection);
        services.AddSkolioAuthorization(configureAdditionalPolicies);
        services.AddSkolioCors(configuration, configureCors);
        return services;
    }

    public static WebApplication UseSkolioServiceDefaults(
        this WebApplication app,
        string serviceName,
        Func<Exception, bool>? isDomainException = null)
    {
        app.UseSkolioCorrelationId(serviceName);
        app.UseSkolioExceptionHandler(serviceName, isDomainException);
        app.UseCors(SkolioCorsExtensions.DevelopmentPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
