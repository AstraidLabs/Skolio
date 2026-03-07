using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Infrastructure.Seeding;

public sealed class OrganizationDevelopmentSeeder(
    OrganizationDbContext dbContext,
    IConfiguration configuration,
    ILogger<OrganizationDevelopmentSeeder> logger)
{
    private static readonly Guid KindergartenSchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ElementarySchoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SecondarySchoolId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid KindergartenSchoolAdminProfileId = Guid.Parse("10000000-0000-0000-0000-000000000101");
    private static readonly Guid ElementarySchoolAdminProfileId = Guid.Parse("10000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondarySchoolAdminProfileId = Guid.Parse("10000000-0000-0000-0000-000000000301");

    private static readonly Guid KindergartenTeacherProfileId = Guid.Parse("10000000-0000-0000-0000-000000000102");
    private static readonly Guid ElementaryTeacherProfileId = Guid.Parse("10000000-0000-0000-0000-000000000202");
    private static readonly Guid SecondaryTeacherProfileId = Guid.Parse("10000000-0000-0000-0000-000000000302");

    private static readonly Guid KindergartenSchoolYearId = Guid.Parse("20000000-0000-0000-0000-000000000101");
    private static readonly Guid ElementarySchoolYearId = Guid.Parse("20000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondarySchoolYearId = Guid.Parse("20000000-0000-0000-0000-000000000301");

    private static readonly Guid ElementaryGradeLevelId = Guid.Parse("21000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondaryGradeLevelId = Guid.Parse("21000000-0000-0000-0000-000000000301");

    private static readonly Guid ElementaryClassRoomId = Guid.Parse("22000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondaryClassRoomId = Guid.Parse("22000000-0000-0000-0000-000000000301");

    private static readonly Guid KindergartenGroupId = Guid.Parse("23000000-0000-0000-0000-000000000101");
    private static readonly Guid ElementaryGroupId = Guid.Parse("23000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondaryGroupId = Guid.Parse("23000000-0000-0000-0000-000000000301");

    private static readonly Guid ElementarySubjectId = Guid.Parse("24000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondarySubjectId = Guid.Parse("24000000-0000-0000-0000-000000000301");

    private static readonly Guid SecondaryFieldOfStudyId = Guid.Parse("25000000-0000-0000-0000-000000000301");

    private static readonly Guid KindergartenTeacherAssignmentId = Guid.Parse("26000000-0000-0000-0000-000000000101");
    private static readonly Guid ElementaryTeacherAssignmentId = Guid.Parse("26000000-0000-0000-0000-000000000201");
    private static readonly Guid SecondaryTeacherAssignmentId = Guid.Parse("26000000-0000-0000-0000-000000000301");

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Organization:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Organization seed is disabled by configuration.");
            return;
        }

        logger.LogInformation("Organization seed started.");

        var kindergarten = await EnsureSchoolAsync(
            KindergartenSchoolId,
            "Skolio Kindergarten Brno",
            SchoolType.Kindergarten,
            KindergartenSchoolAdminProfileId,
            cancellationToken);

        var elementary = await EnsureSchoolAsync(
            ElementarySchoolId,
            "Skolio Elementary Prague",
            SchoolType.ElementarySchool,
            ElementarySchoolAdminProfileId,
            cancellationToken);

        var secondary = await EnsureSchoolAsync(
            SecondarySchoolId,
            "Skolio Secondary Ostrava",
            SchoolType.SecondarySchool,
            SecondarySchoolAdminProfileId,
            cancellationToken);

        await EnsureSchoolYearAsync(kindergarten, KindergartenSchoolYearId, cancellationToken);
        await EnsureSchoolYearAsync(elementary, ElementarySchoolYearId, cancellationToken);
        await EnsureSchoolYearAsync(secondary, SecondarySchoolYearId, cancellationToken);

        var elementaryGradeLevel = await EnsureGradeLevelAsync(elementary, ElementaryGradeLevelId, 1, "1. rocnik", cancellationToken);
        var secondaryGradeLevel = await EnsureGradeLevelAsync(secondary, SecondaryGradeLevelId, 1, "1. rocnik", cancellationToken);

        var elementaryClassRoom = await EnsureClassRoomAsync(elementary, elementaryGradeLevel, ElementaryClassRoomId, "1A", "Trida 1A", cancellationToken);
        var secondaryClassRoom = await EnsureClassRoomAsync(secondary, secondaryGradeLevel, SecondaryClassRoomId, "S1A", "Trida S1A", cancellationToken);

        var kindergartenGroup = await EnsureTeachingGroupAsync(kindergarten, KindergartenGroupId, null, "Berusky", true, cancellationToken);
        var elementaryGroup = await EnsureTeachingGroupAsync(elementary, ElementaryGroupId, elementaryClassRoom.Id, "Skupina 1A", false, cancellationToken);
        var secondaryGroup = await EnsureTeachingGroupAsync(secondary, SecondaryGroupId, secondaryClassRoom.Id, "Skupina S1A", false, cancellationToken);

        var elementarySubject = await EnsureSubjectAsync(elementary, ElementarySubjectId, "MAT", "Matematika", cancellationToken);
        var secondarySubject = await EnsureSubjectAsync(secondary, SecondarySubjectId, "INF", "Informatika", cancellationToken);
        await EnsureSecondaryFieldOfStudyAsync(secondary, SecondaryFieldOfStudyId, "IT", "Informacni technologie", cancellationToken);

        await EnsureTeacherAssignmentAsync(
            KindergartenTeacherAssignmentId,
            kindergarten.Id,
            KindergartenTeacherProfileId,
            TeacherAssignmentScope.DailyOperations,
            classRoomId: null,
            teachingGroupId: kindergartenGroup.Id,
            subjectId: null,
            cancellationToken);

        await EnsureTeacherAssignmentAsync(
            ElementaryTeacherAssignmentId,
            elementary.Id,
            ElementaryTeacherProfileId,
            TeacherAssignmentScope.Subject,
            classRoomId: elementaryClassRoom.Id,
            teachingGroupId: elementaryGroup.Id,
            subjectId: elementarySubject.Id,
            cancellationToken);

        await EnsureTeacherAssignmentAsync(
            SecondaryTeacherAssignmentId,
            secondary.Id,
            SecondaryTeacherProfileId,
            TeacherAssignmentScope.Subject,
            classRoomId: secondaryClassRoom.Id,
            teachingGroupId: secondaryGroup.Id,
            subjectId: secondarySubject.Id,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Organization seed completed.");
    }

    private async Task<School> EnsureSchoolAsync(
        Guid schoolId,
        string schoolName,
        SchoolType schoolType,
        Guid schoolAdministratorUserProfileId,
        CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == schoolId, cancellationToken);
        if (school is null)
        {
            school = School.Create(schoolId, schoolName, schoolType);
            school.AssignSchoolAdministrator(schoolAdministratorUserProfileId);
            dbContext.Schools.Add(school);
            logger.LogInformation("Seed school created: {SchoolName}", schoolName);
            return school;
        }

        school.Rename(schoolName);
        school.ChangeSchoolType(schoolType);
        school.Activate();
        school.AssignSchoolAdministrator(schoolAdministratorUserProfileId);
        logger.LogInformation("Seed school already exists and was refreshed: {SchoolName}", schoolName);
        return school;
    }

    private async Task EnsureSchoolYearAsync(School school, Guid schoolYearId, CancellationToken cancellationToken)
    {
        var schoolYear = await dbContext.SchoolYears.FirstOrDefaultAsync(x => x.Id == schoolYearId, cancellationToken);
        if (schoolYear is null)
        {
            schoolYear = SchoolYear.Create(schoolYearId, school.Id, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30));
            dbContext.SchoolYears.Add(schoolYear);
            logger.LogInformation("Seed school year created for school {SchoolId}.", school.Id);
            return;
        }

        schoolYear.UpdatePeriod(new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30));
        logger.LogInformation("Seed school year already exists and was refreshed for school {SchoolId}.", school.Id);
    }

    private async Task<GradeLevel> EnsureGradeLevelAsync(School school, Guid gradeLevelId, int level, string displayName, CancellationToken cancellationToken)
    {
        var gradeLevel = await dbContext.GradeLevels.FirstOrDefaultAsync(x => x.Id == gradeLevelId, cancellationToken);
        if (gradeLevel is null)
        {
            gradeLevel = GradeLevel.Create(gradeLevelId, school.Id, school.SchoolType, level, displayName);
            dbContext.GradeLevels.Add(gradeLevel);
            logger.LogInformation("Seed grade level created: {DisplayName} ({SchoolId}).", displayName, school.Id);
            return gradeLevel;
        }

        gradeLevel.Update(school.SchoolType, level, displayName);
        logger.LogInformation("Seed grade level already exists and was refreshed: {DisplayName} ({SchoolId}).", displayName, school.Id);
        return gradeLevel;
    }

    private async Task<ClassRoom> EnsureClassRoomAsync(School school, GradeLevel gradeLevel, Guid classRoomId, string code, string displayName, CancellationToken cancellationToken)
    {
        var classRoom = await dbContext.ClassRooms.FirstOrDefaultAsync(x => x.Id == classRoomId, cancellationToken);
        if (classRoom is null)
        {
            classRoom = ClassRoom.Create(classRoomId, school.Id, gradeLevel.Id, school.SchoolType, code, displayName);
            dbContext.ClassRooms.Add(classRoom);
            logger.LogInformation("Seed classroom created: {Code} ({SchoolId}).", code, school.Id);
            return classRoom;
        }

        classRoom.OverrideForPlatformSupport(code, displayName);
        logger.LogInformation("Seed classroom already exists and was refreshed: {Code} ({SchoolId}).", code, school.Id);
        return classRoom;
    }

    private async Task<TeachingGroup> EnsureTeachingGroupAsync(School school, Guid groupId, Guid? classRoomId, string name, bool isDailyOperationsGroup, CancellationToken cancellationToken)
    {
        var group = await dbContext.TeachingGroups.FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null)
        {
            group = TeachingGroup.Create(groupId, school.Id, classRoomId, name, isDailyOperationsGroup);
            dbContext.TeachingGroups.Add(group);
            logger.LogInformation("Seed teaching group created: {Name} ({SchoolId}).", name, school.Id);
            return group;
        }

        group.OverrideForPlatformSupport(classRoomId, name, isDailyOperationsGroup);
        logger.LogInformation("Seed teaching group already exists and was refreshed: {Name} ({SchoolId}).", name, school.Id);
        return group;
    }

    private async Task<Subject> EnsureSubjectAsync(School school, Guid subjectId, string code, string name, CancellationToken cancellationToken)
    {
        var subject = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == subjectId, cancellationToken);
        if (subject is null)
        {
            subject = Subject.Create(subjectId, school.Id, code, name);
            dbContext.Subjects.Add(subject);
            logger.LogInformation("Seed subject created: {Code} ({SchoolId}).", code, school.Id);
            return subject;
        }

        subject.OverrideForPlatformSupport(code, name);
        logger.LogInformation("Seed subject already exists and was refreshed: {Code} ({SchoolId}).", code, school.Id);
        return subject;
    }

    private async Task EnsureSecondaryFieldOfStudyAsync(School school, Guid fieldId, string code, string name, CancellationToken cancellationToken)
    {
        var field = await dbContext.SecondaryFieldsOfStudy.FirstOrDefaultAsync(x => x.Id == fieldId, cancellationToken);
        if (field is null)
        {
            field = SecondaryFieldOfStudy.Create(fieldId, school.Id, school.SchoolType, code, name);
            dbContext.SecondaryFieldsOfStudy.Add(field);
            logger.LogInformation("Seed secondary field of study created: {Code} ({SchoolId}).", code, school.Id);
            return;
        }

        field.Update(school.SchoolType, code, name);
        logger.LogInformation("Seed secondary field of study already exists and was refreshed: {Code} ({SchoolId}).", code, school.Id);
    }

    private async Task EnsureTeacherAssignmentAsync(
        Guid assignmentId,
        Guid schoolId,
        Guid teacherUserId,
        TeacherAssignmentScope scope,
        Guid? classRoomId,
        Guid? teachingGroupId,
        Guid? subjectId,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.TeacherAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is not null)
        {
            logger.LogInformation("Seed teacher assignment already exists: {AssignmentId}.", assignmentId);
            return;
        }

        dbContext.TeacherAssignments.Add(TeacherAssignment.Create(
            assignmentId,
            schoolId,
            teacherUserId,
            scope,
            classRoomId,
            teachingGroupId,
            subjectId));

        logger.LogInformation("Seed teacher assignment created: {AssignmentId}.", assignmentId);
    }
}
