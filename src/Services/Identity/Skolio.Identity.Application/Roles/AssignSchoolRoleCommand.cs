using MediatR;
using Skolio.Identity.Application.Contracts;

namespace Skolio.Identity.Application.Roles;

public sealed record AssignSchoolRoleCommand(Guid UserProfileId, Guid SchoolId, string RoleCode) : IRequest<SchoolRoleAssignmentContract>;
