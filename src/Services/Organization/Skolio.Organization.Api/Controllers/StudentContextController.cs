using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/student-context")]
public sealed class StudentContextController(OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<StudentContextContract>> Context([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var school = await dbContext.Schools.Include(x => x.SchoolOperator).Include(x => x.Founder).FirstOrDefaultAsync(x => x.Id == schoolId, cancellationToken);
        if (school is null) return NotFound();

        var schoolYearIds = SchoolScope.GetStudentSchoolYearIds(User);
        var classRoomIds = SchoolScope.GetStudentClassRoomIds(User);
        var groupIds = SchoolScope.GetStudentTeachingGroupIds(User);
        var subjectIds = SchoolScope.GetStudentSubjectIds(User);
        var gradeLevelIds = SchoolScope.GetStudentGradeLevelIds(User);
        var fieldOfStudyIds = SchoolScope.GetStudentFieldOfStudyIds(User);

        IReadOnlyCollection<SchoolYearContract> schoolYears = schoolYearIds.Count == 0
            ? Array.Empty<SchoolYearContract>()
            : await dbContext.SchoolYears
                .Where(x => x.SchoolId == schoolId && schoolYearIds.Contains(x.Id))
                .OrderByDescending(x => x.Period.StartDate)
                .Select(x => new SchoolYearContract(x.Id, x.SchoolId, x.Label, x.Period.StartDate, x.Period.EndDate))
                .ToListAsync(cancellationToken);

        IReadOnlyCollection<ClassRoomContract> classRooms = classRoomIds.Count == 0
            ? Array.Empty<ClassRoomContract>()
            : await dbContext.ClassRooms
                .Where(x => x.SchoolId == schoolId && classRoomIds.Contains(x.Id))
                .OrderBy(x => x.DisplayName)
                .Select(x => new ClassRoomContract(x.Id, x.SchoolId, x.GradeLevelId, x.Code, x.DisplayName))
                .ToListAsync(cancellationToken);

        IReadOnlyCollection<TeachingGroupContract> teachingGroups = groupIds.Count == 0
            ? Array.Empty<TeachingGroupContract>()
            : await dbContext.TeachingGroups
                .Where(x => x.SchoolId == schoolId && groupIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new TeachingGroupContract(x.Id, x.SchoolId, x.ClassRoomId, x.Name, x.IsDailyOperationsGroup))
                .ToListAsync(cancellationToken);

        IReadOnlyCollection<SubjectContract> subjects = subjectIds.Count == 0
            ? Array.Empty<SubjectContract>()
            : await dbContext.Subjects
                .Where(x => x.SchoolId == schoolId && subjectIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new SubjectContract(x.Id, x.SchoolId, x.Code, x.Name))
                .ToListAsync(cancellationToken);

        IReadOnlyCollection<GradeLevelContract> gradeLevels = gradeLevelIds.Count == 0
            ? Array.Empty<GradeLevelContract>()
            : await dbContext.GradeLevels
                .Where(x => x.SchoolId == schoolId && gradeLevelIds.Contains(x.Id))
                .OrderBy(x => x.Level)
                .Select(x => new GradeLevelContract(x.Id, x.SchoolId, x.Level, x.DisplayName))
                .ToListAsync(cancellationToken);

        IReadOnlyCollection<SecondaryFieldOfStudyContract> fields = school.SchoolType != SchoolType.SecondarySchool || fieldOfStudyIds.Count == 0
            ? Array.Empty<SecondaryFieldOfStudyContract>()
            : await dbContext.SecondaryFieldsOfStudy
                .Where(x => x.SchoolId == schoolId && fieldOfStudyIds.Contains(x.Id))
                .OrderBy(x => x.Code)
                .Select(x => new SecondaryFieldOfStudyContract(x.Id, x.SchoolId, x.Code, x.Name))
                .ToListAsync(cancellationToken);

        var schoolContract = new SchoolContract(
            school.Id,
            school.Name,
            school.SchoolType,
            school.SchoolKind,
            school.SchoolIzo,
            school.SchoolEmail,
            school.SchoolPhone,
            school.SchoolWebsite,
            new AddressContract(school.MainAddress.Street, school.MainAddress.City, school.MainAddress.PostalCode, school.MainAddress.Country),
            school.EducationLocationsSummary,
            school.RegistryEntryDate,
            school.EducationStartDate,
            school.MaxStudentCapacity,
            school.TeachingLanguage,
            school.SchoolOperatorId,
            school.FounderId,
            school.PlatformStatus,
            school.IsActive,
            school.SchoolAdministratorUserProfileId,
            school.SchoolOperator is null
                ? null
                : new SchoolOperatorContract(
                    school.SchoolOperator.Id,
                    school.SchoolOperator.LegalEntityName,
                    school.SchoolOperator.LegalForm,
                    school.SchoolOperator.CompanyNumberIco,
                    new AddressContract(
                        school.SchoolOperator.RegisteredOfficeAddress.Street,
                        school.SchoolOperator.RegisteredOfficeAddress.City,
                        school.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                        school.SchoolOperator.RegisteredOfficeAddress.Country),
                    school.SchoolOperator.ResortIdentifier,
                    school.SchoolOperator.DirectorSummary,
                    school.SchoolOperator.StatutoryBodySummary),
            school.Founder is null
                ? null
                : new FounderContract(
                    school.Founder.Id,
                    school.Founder.FounderType,
                    school.Founder.FounderCategory,
                    school.Founder.FounderName,
                    school.Founder.FounderLegalForm,
                    school.Founder.FounderIco,
                    new AddressContract(
                        school.Founder.FounderAddress.Street,
                        school.Founder.FounderAddress.City,
                        school.Founder.FounderAddress.PostalCode,
                        school.Founder.FounderAddress.Country),
                    school.Founder.FounderEmail));

        return Ok(new StudentContextContract(
            schoolContract,
            schoolYears,
            classRooms,
            teachingGroups,
            subjects,
            gradeLevels,
            fields));
    }

    public sealed record StudentContextContract(
        SchoolContract School,
        IReadOnlyCollection<SchoolYearContract> SchoolYears,
        IReadOnlyCollection<ClassRoomContract> ClassRooms,
        IReadOnlyCollection<TeachingGroupContract> TeachingGroups,
        IReadOnlyCollection<SubjectContract> Subjects,
        IReadOnlyCollection<GradeLevelContract> GradeLevels,
        IReadOnlyCollection<SecondaryFieldOfStudyContract> SecondaryFieldsOfStudy);
}
