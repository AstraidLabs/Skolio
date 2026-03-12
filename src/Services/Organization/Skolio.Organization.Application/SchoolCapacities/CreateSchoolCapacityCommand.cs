using MediatR;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.SchoolCapacities;

public sealed record CreateSchoolCapacityCommand(
    Guid SchoolId,
    SchoolCapacityType CapacityType,
    int MaxCapacity,
    string? Description) : IRequest<SchoolCapacityContract>;
