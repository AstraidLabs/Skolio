using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.Shared.Security;

public static class SkolioSecuritySetup
{
    /// <summary>
    /// Registers JWT Bearer authentication with the correct claim type mapping for .NET 10+.
    /// Sets RoleClaimType = "role" so that User.IsInRole() and RequireRole() correctly resolve
    /// the short-form "role" JWT claim emitted by OpenIddict.
    /// </summary>
    public static IServiceCollection AddSkolioJwtBearer(
        this IServiceCollection services,
        string authority,
        string audience,
        bool requireHttpsMetadata = true)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                // In .NET 8+, JwtBearer defaults to MapInboundClaims = false.
                // OpenIddict emits roles as short-form "role" claims in the JWT.
                // Without this setting, User.IsInRole() and RequireRole() silently fail with 403
                // because they check for ClaimTypes.Role (long URI form) by default.
                options.TokenValidationParameters.RoleClaimType = "role";
            });

        return services;
    }

    /// <summary>
    /// Registers all Skolio authorization policies.
    /// Centralised here so policy definitions stay in sync across all services.
    /// </summary>
    public static IServiceCollection AddSkolioAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(SkolioPolicies.PlatformAdministration,
                policy => policy.RequireRole("PlatformAdministrator"));

            options.AddPolicy(SkolioPolicies.PlatformAdminOverride,
                policy => policy.RequireRole("PlatformAdministrator"));

            options.AddPolicy(SkolioPolicies.SharedAdministration,
                policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));

            options.AddPolicy(SkolioPolicies.SchoolAdministrationOnly,
                policy => policy.RequireRole("SchoolAdministrator"));

            options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministrationOnly,
                policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));

            options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead,
                policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));

            options.AddPolicy(SkolioPolicies.StudentSelfService,
                policy => policy.RequireRole("Student"));
        });

        return services;
    }
}
