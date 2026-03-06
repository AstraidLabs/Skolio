using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.TeachingGroups;

public sealed record CreateTeachingGroupCommand(Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup) : IRequest<TeachingGroupContract>;
