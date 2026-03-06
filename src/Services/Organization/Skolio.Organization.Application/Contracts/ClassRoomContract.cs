namespace Skolio.Organization.Application.Contracts;

public sealed record ClassRoomContract(Guid Id, Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
