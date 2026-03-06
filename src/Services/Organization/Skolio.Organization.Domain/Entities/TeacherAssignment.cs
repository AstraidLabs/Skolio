using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class TeacherAssignment
{
    private TeacherAssignment(
        Guid id,
        Guid schoolId,
        Guid teacherUserId,
        TeacherAssignmentScope scope,
        Guid? classRoomId,
        Guid? teachingGroupId,
        Guid? subjectId)
    {
        Id = id;
        SchoolId = schoolId;
        TeacherUserId = teacherUserId;
        Scope = scope;
        ClassRoomId = classRoomId;
        TeachingGroupId = teachingGroupId;
        SubjectId = subjectId;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid TeacherUserId { get; }
    public TeacherAssignmentScope Scope { get; }
    public Guid? ClassRoomId { get; }
    public Guid? TeachingGroupId { get; }
    public Guid? SubjectId { get; }

    public static TeacherAssignment Create(
        Guid id,
        Guid schoolId,
        Guid teacherUserId,
        TeacherAssignmentScope scope,
        Guid? classRoomId,
        Guid? teachingGroupId,
        Guid? subjectId)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty || teacherUserId == Guid.Empty)
        {
            throw new OrganizationDomainException("Teacher assignment id, school id and teacher user id are required.");
        }

        if (scope == TeacherAssignmentScope.ClassRoom && classRoomId is null)
        {
            throw new OrganizationDomainException("Class room assignment must include class room id.");
        }

        if (scope == TeacherAssignmentScope.TeachingGroup && teachingGroupId is null)
        {
            throw new OrganizationDomainException("Teaching group assignment must include teaching group id.");
        }

        if (scope == TeacherAssignmentScope.Subject && subjectId is null)
        {
            throw new OrganizationDomainException("Subject assignment must include subject id.");
        }

        return new TeacherAssignment(id, schoolId, teacherUserId, scope, classRoomId, teachingGroupId, subjectId);
    }
}
