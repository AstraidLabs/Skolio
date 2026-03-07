using System.Security.Claims;

namespace Skolio.Organization.Api.Auth;

internal static class SchoolScope
{
    public static bool IsPlatformAdministrator(ClaimsPrincipal user) => user.IsInRole("PlatformAdministrator");

    public static bool IsParent(ClaimsPrincipal user) => user.IsInRole("Parent");

    public static Guid ResolveActorUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public static HashSet<Guid> GetScopedSchoolIds(ClaimsPrincipal user)
    {
        return user.FindAll("school_id")
            .Select(x => Guid.TryParse(x.Value, out var schoolId) ? schoolId : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();
    }

    public static HashSet<Guid> GetLinkedStudentIds(ClaimsPrincipal user)
    {
        return user.FindAll("linked_student_id")
            .Select(x => Guid.TryParse(x.Value, out var studentId) ? studentId : Guid.Empty)
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
