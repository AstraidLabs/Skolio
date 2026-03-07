namespace Skolio.Organization.Application.Contracts;

public sealed record SecondaryFieldOfStudyContract(Guid Id, Guid SchoolId, string Code, string Name);
