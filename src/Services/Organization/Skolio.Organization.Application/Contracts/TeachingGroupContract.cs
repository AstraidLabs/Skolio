namespace Skolio.Organization.Application.Contracts;

public sealed record TeachingGroupContract(Guid Id, Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
