namespace Skolio.Organization.Application.Contracts;

public sealed record GradeLevelContract(Guid Id, Guid SchoolId, int Level, string DisplayName);
