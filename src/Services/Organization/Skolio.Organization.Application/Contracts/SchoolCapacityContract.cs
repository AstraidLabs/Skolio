using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolCapacityContract(
    Guid Id,
    Guid SchoolId,
    SchoolCapacityType CapacityType,
    int MaxCapacity,
    string? Description);
