using MediatR;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Schools;

public sealed record CreateSchoolCommand(string Name, SchoolType SchoolType) : IRequest<SchoolContract>;
