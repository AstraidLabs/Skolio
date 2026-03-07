using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolYear
{
    private SchoolYear()
    {
    }

    private SchoolYear(Guid id, Guid schoolId, string label, SchoolYearPeriod period)
    {
        Id = id;
        SchoolId = schoolId;
        SetLabel(label);
        Period = period;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public SchoolYearPeriod Period { get; private set; } = default!;

    public static SchoolYear Create(Guid id, Guid schoolId, string label, DateOnly startDate, DateOnly endDate)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("School year and school ids are required.");
        }

        return new SchoolYear(id, schoolId, label, new SchoolYearPeriod(startDate, endDate));
    }

    public void UpdatePeriod(DateOnly startDate, DateOnly endDate) => Period = new SchoolYearPeriod(startDate, endDate);

    private void SetLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new OrganizationDomainException("School year label is required.");
        }

        Label = label.Trim();
    }
}
