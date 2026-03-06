using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolContract(Guid Id, string Name, SchoolType SchoolType);
