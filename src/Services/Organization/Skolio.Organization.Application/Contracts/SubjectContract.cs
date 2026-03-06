namespace Skolio.Organization.Application.Contracts;

public sealed record SubjectContract(Guid Id, Guid SchoolId, string Code, string Name);
