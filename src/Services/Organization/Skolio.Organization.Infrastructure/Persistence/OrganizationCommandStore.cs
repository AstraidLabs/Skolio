using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence;

public sealed class OrganizationCommandStore(OrganizationDbContext dbContext) : IOrganizationCommandStore
{
    public Task AddSchoolAsync(School school, CancellationToken cancellationToken) => dbContext.Schools.AddAsync(school, cancellationToken).AsTask();
    public Task AddSchoolOperatorAsync(SchoolOperator schoolOperator, CancellationToken cancellationToken) => dbContext.SchoolOperators.AddAsync(schoolOperator, cancellationToken).AsTask();
    public Task AddFounderAsync(Founder founder, CancellationToken cancellationToken) => dbContext.Founders.AddAsync(founder, cancellationToken).AsTask();
    public Task AddSchoolYearAsync(SchoolYear schoolYear, CancellationToken cancellationToken) => dbContext.SchoolYears.AddAsync(schoolYear, cancellationToken).AsTask();
    public Task AddClassRoomAsync(ClassRoom classRoom, CancellationToken cancellationToken) => dbContext.ClassRooms.AddAsync(classRoom, cancellationToken).AsTask();
    public Task AddTeachingGroupAsync(TeachingGroup teachingGroup, CancellationToken cancellationToken) => dbContext.TeachingGroups.AddAsync(teachingGroup, cancellationToken).AsTask();
    public Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken) => dbContext.Subjects.AddAsync(subject, cancellationToken).AsTask();
    public Task AddTeacherAssignmentAsync(TeacherAssignment teacherAssignment, CancellationToken cancellationToken) => dbContext.TeacherAssignments.AddAsync(teacherAssignment, cancellationToken).AsTask();
    public Task AddSchoolPlaceOfEducationAsync(SchoolPlaceOfEducation place, CancellationToken cancellationToken) => dbContext.SchoolPlacesOfEducation.AddAsync(place, cancellationToken).AsTask();
    public Task AddSchoolCapacityAsync(SchoolCapacity capacity, CancellationToken cancellationToken) => dbContext.SchoolCapacities.AddAsync(capacity, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
