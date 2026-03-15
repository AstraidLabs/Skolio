namespace Skolio.ServiceDefaults.Authorization;

public static class SkolioPolicies
{
    public const string PlatformAdministration = "platform-administration";
    public const string PlatformAdminOverride = "platform-admin-override";
    public const string SharedAdministration = "shared-administration";
    public const string SchoolAdministrationOnly = "school-administration-only";
    public const string TeacherOrSchoolAdministrationOnly = "teacher-or-school-administration-only";
    public const string ParentStudentTeacherRead = "parent-student-teacher-read";
    public const string StudentSelfService = "student-self-service";
    public const string ServiceAccess = "service-access";
}
