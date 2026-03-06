namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolYearContract(Guid Id, Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
