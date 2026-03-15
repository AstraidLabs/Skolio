using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.ServiceDefaults.Middleware;

public static class SkolioCorsExtensions
{
    public const string DevelopmentPolicyName = "SkolioDevelopment";

    private static readonly string[] DefaultOrigins = ["http://localhost:8080", "http://localhost:5173"];

    public static IServiceCollection AddSkolioCors(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<CorsPolicyBuilder>? configurePolicyBuilder = null)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(DevelopmentPolicyName, policy =>
            {
                var origins = configuration?.GetSection("Cors:Origins").Get<string[]>() ?? DefaultOrigins;
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                configurePolicyBuilder?.Invoke(policy);
            });
        });

        return services;
    }
}
