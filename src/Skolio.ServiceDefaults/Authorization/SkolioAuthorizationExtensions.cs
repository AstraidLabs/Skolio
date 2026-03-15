using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Skolio.ServiceDefaults.Authorization;

public static class SkolioAuthorizationExtensions
{
    public static IServiceCollection AddSkolioAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureAdditional = null)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(SkolioPolicies.PlatformAdministration, policy =>
                policy.RequireRole(SkolioRoles.PlatformAdministrator));

            options.AddPolicy(SkolioPolicies.SharedAdministration, policy =>
                policy.RequireRole(SkolioRoles.PlatformAdministrator, SkolioRoles.SchoolAdministrator));

            options.AddPolicy(SkolioPolicies.SchoolAdministrationOnly, policy =>
                policy.RequireRole(SkolioRoles.SchoolAdministrator));

            options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministrationOnly, policy =>
                policy.RequireRole(SkolioRoles.PlatformAdministrator, SkolioRoles.SchoolAdministrator, SkolioRoles.Teacher));

            options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy =>
                policy.RequireRole(SkolioRoles.PlatformAdministrator, SkolioRoles.SchoolAdministrator, SkolioRoles.Teacher, SkolioRoles.Parent, SkolioRoles.Student));

            options.AddPolicy(SkolioPolicies.StudentSelfService, policy =>
                policy.RequireRole(SkolioRoles.Student));

            options.AddPolicy(SkolioPolicies.PlatformAdminOverride, policy =>
                policy.RequireRole(SkolioRoles.PlatformAdministrator));

            options.AddPolicy(SkolioPolicies.ServiceAccess, policy =>
                policy.RequireRole(SkolioRoles.Service));

            configureAdditional?.Invoke(options);
        });

        return services;
    }
}
