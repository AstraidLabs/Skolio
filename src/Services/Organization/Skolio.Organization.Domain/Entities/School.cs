using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class School
{
    private readonly List<SchoolYear> _schoolYears = [];

    private School(Guid id, string name, SchoolType schoolType)
    {
        Id = id;
        SetName(name);
        SchoolType = schoolType;
    }

    public Guid Id { get; }
    public string Name { get; private set; } = string.Empty;
    public SchoolType SchoolType { get; }
    public IReadOnlyCollection<SchoolYear> SchoolYears => _schoolYears;

    public static School Create(Guid id, string name, SchoolType schoolType)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School id is required.");
        }

        return new School(id, name, schoolType);
    }

    public void Rename(string name) => SetName(name);

    public SchoolYear AddSchoolYear(Guid schoolYearId, string label, DateOnly startDate, DateOnly endDate)
    {
        if (_schoolYears.Any(x => x.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
        {
            throw new OrganizationDomainException("School year label must be unique within school.");
        }

        var schoolYear = SchoolYear.Create(schoolYearId, Id, label, startDate, endDate);
        _schoolYears.Add(schoolYear);
        return schoolYear;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("School name is required.");
        }

        Name = name.Trim();
    }
}
