using MediatR;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.TeacherAssignments;

public sealed record AssignTeacherCommand(
    Guid SchoolId,
    Guid TeacherUserId,
    TeacherAssignmentScope Scope,
    Guid? ClassRoomId,
    Guid? TeachingGroupId,
    Guid? SubjectId) : IRequest<TeacherAssignmentContract>;
