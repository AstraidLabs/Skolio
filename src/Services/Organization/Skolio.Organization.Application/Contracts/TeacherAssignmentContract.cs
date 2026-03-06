using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record TeacherAssignmentContract(
    Guid Id,
    Guid SchoolId,
    Guid TeacherUserId,
    TeacherAssignmentScope Scope,
    Guid? ClassRoomId,
    Guid? TeachingGroupId,
    Guid? SubjectId);
