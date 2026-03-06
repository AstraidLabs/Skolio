using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.ValueObjects;

public sealed record SchoolYearPeriod
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public SchoolYearPeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new OrganizationDomainException("School year end date must be after start date.");
        }

        StartDate = startDate;
        EndDate = endDate;
    }
}
