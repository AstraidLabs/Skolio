using System.Security.Claims;

namespace Skolio.Administration.Api.Auth;

internal static class SchoolScope
{
    public static bool IsPlatformAdministrator(ClaimsPrincipal user) => user.IsInRole("PlatformAdministrator");

    public static HashSet<Guid> GetScopedSchoolIds(ClaimsPrincipal user)
    {
        return user.FindAll("school_id")
            .Select(x => Guid.TryParse(x.Value, out var schoolId) ? schoolId : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();
    }

    public static bool HasSchoolAccess(ClaimsPrincipal user, Guid schoolId)
    {
        if (IsPlatformAdministrator(user)) return true;
        var scopedSchoolIds = GetScopedSchoolIds(user);
        return scopedSchoolIds.Contains(schoolId);
    }
}
