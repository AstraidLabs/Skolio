using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolPlaceOfEducation
{
    private SchoolPlaceOfEducation()
    {
    }

    private SchoolPlaceOfEducation(
        Guid id,
        Guid schoolId,
        string name,
        Address address,
        string? description,
        bool isPrimary)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School place of education id is required.");
        }

        if (schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("School place of education school id is required.");
        }

        Id = id;
        SchoolId = schoolId;
        SetName(name);
        Address = address;
        Description = NormalizeOptional(description);
        IsPrimary = isPrimary;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = Address.Create("Unknown", "Unknown", "00000", "CZ");
    public string? Description { get; private set; }
    public bool IsPrimary { get; private set; }

    public School School { get; private set; } = null!;

    public static SchoolPlaceOfEducation Create(
        Guid id,
        Guid schoolId,
        string name,
        Address address,
        string? description,
        bool isPrimary)
        => new(id, schoolId, name, address, description, isPrimary);

    public void Update(
        string name,
        Address address,
        string? description,
        bool isPrimary)
    {
        SetName(name);
        Address = address;
        Description = NormalizeOptional(description);
        IsPrimary = isPrimary;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("School place of education name is required.");
        }

        Name = name.Trim();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
