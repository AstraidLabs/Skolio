using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.Abstractions;

public interface IOrganizationCommandStore
{
    Task AddSchoolAsync(School school, CancellationToken cancellationToken);
    Task AddSchoolOperatorAsync(SchoolOperator schoolOperator, CancellationToken cancellationToken);
    Task AddFounderAsync(Founder founder, CancellationToken cancellationToken);
    Task AddSchoolYearAsync(SchoolYear schoolYear, CancellationToken cancellationToken);
    Task AddClassRoomAsync(ClassRoom classRoom, CancellationToken cancellationToken);
    Task AddTeachingGroupAsync(TeachingGroup teachingGroup, CancellationToken cancellationToken);
    Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken);
    Task AddTeacherAssignmentAsync(TeacherAssignment teacherAssignment, CancellationToken cancellationToken);
    Task AddSchoolPlaceOfEducationAsync(SchoolPlaceOfEducation place, CancellationToken cancellationToken);
    Task AddSchoolCapacityAsync(SchoolCapacity capacity, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
