using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.ClassRooms;

public sealed record CreateClassRoomCommand(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName) : IRequest<ClassRoomContract>;
