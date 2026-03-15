using System.Security.Claims;

namespace Skolio.ServiceDefaults.Authorization;

public static class SchoolScope
{
    public static bool IsPlatformAdministrator(ClaimsPrincipal user) => user.IsInRole(SkolioRoles.PlatformAdministrator);

    public static bool IsParent(ClaimsPrincipal user) => user.IsInRole(SkolioRoles.Parent);

    public static bool IsStudent(ClaimsPrincipal user) => user.IsInRole(SkolioRoles.Student);

    public static Guid ResolveActorUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public static HashSet<Guid> GetScopedSchoolIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.SchoolId);

    public static HashSet<Guid> GetLinkedStudentIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.LinkedStudentId);

    public static HashSet<Guid> GetStudentClassRoomIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.ClassRoomId);

    public static HashSet<Guid> GetStudentTeachingGroupIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.TeachingGroupId);

    public static HashSet<Guid> GetStudentSubjectIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.SubjectId);

    public static HashSet<Guid> GetStudentSchoolYearIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.SchoolYearId);

    public static HashSet<Guid> GetStudentGradeLevelIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.GradeLevelId);

    public static HashSet<Guid> GetStudentFieldOfStudyIds(ClaimsPrincipal user) => GetScopedIds(user, SkolioClaimTypes.FieldOfStudyId);

    public static bool HasSchoolAccess(ClaimsPrincipal user, Guid schoolId)
    {
        if (IsPlatformAdministrator(user)) return true;
        var scopedSchoolIds = GetScopedSchoolIds(user);
        return scopedSchoolIds.Contains(schoolId);
    }

    private static HashSet<Guid> GetScopedIds(ClaimsPrincipal user, string claimType)
    {
        return user.FindAll(claimType)
            .Select(x => Guid.TryParse(x.Value, out var value) ? value : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();
    }
}
