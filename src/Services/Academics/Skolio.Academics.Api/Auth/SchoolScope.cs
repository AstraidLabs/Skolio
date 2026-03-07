using System.Security.Claims;

namespace Skolio.Academics.Api.Auth;

internal static class SchoolScope
{
    public static bool IsPlatformAdministrator(ClaimsPrincipal user) => user.IsInRole("PlatformAdministrator");

    public static bool IsParent(ClaimsPrincipal user) => user.IsInRole("Parent");

    public static bool IsStudent(ClaimsPrincipal user) => user.IsInRole("Student");

    public static Guid ResolveActorUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public static HashSet<Guid> GetScopedSchoolIds(ClaimsPrincipal user) => GetScopedIds(user, "school_id");

    public static HashSet<Guid> GetLinkedStudentIds(ClaimsPrincipal user) => GetScopedIds(user, "linked_student_id");

    public static HashSet<Guid> GetStudentClassRoomIds(ClaimsPrincipal user) => GetScopedIds(user, "class_room_id");

    public static HashSet<Guid> GetStudentTeachingGroupIds(ClaimsPrincipal user) => GetScopedIds(user, "teaching_group_id");

    public static HashSet<Guid> GetStudentSubjectIds(ClaimsPrincipal user) => GetScopedIds(user, "subject_id");

    public static HashSet<Guid> GetStudentSchoolYearIds(ClaimsPrincipal user) => GetScopedIds(user, "school_year_id");

    public static HashSet<Guid> GetStudentGradeLevelIds(ClaimsPrincipal user) => GetScopedIds(user, "grade_level_id");

    public static HashSet<Guid> GetStudentFieldOfStudyIds(ClaimsPrincipal user) => GetScopedIds(user, "field_of_study_id");

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
