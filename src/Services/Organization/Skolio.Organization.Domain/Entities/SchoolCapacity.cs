using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolCapacity
{
    private SchoolCapacity()
    {
    }

    private SchoolCapacity(
        Guid id,
        Guid schoolId,
        SchoolCapacityType capacityType,
        int maxCapacity,
        string? description)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School capacity id is required.");
        }

        if (schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("School capacity school id is required.");
        }

        if (maxCapacity <= 0)
        {
            throw new OrganizationDomainException("School capacity max capacity must be greater than zero.");
        }

        Id = id;
        SchoolId = schoolId;
        CapacityType = capacityType;
        MaxCapacity = maxCapacity;
        Description = NormalizeOptional(description);
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public SchoolCapacityType CapacityType { get; private set; }
    public int MaxCapacity { get; private set; }
    public string? Description { get; private set; }

    public School School { get; private set; } = null!;

    public static SchoolCapacity Create(
        Guid id,
        Guid schoolId,
        SchoolCapacityType capacityType,
        int maxCapacity,
        string? description)
        => new(id, schoolId, capacityType, maxCapacity, description);

    public void Update(
        SchoolCapacityType capacityType,
        int maxCapacity,
        string? description)
    {
        if (maxCapacity <= 0)
        {
            throw new OrganizationDomainException("School capacity max capacity must be greater than zero.");
        }

        CapacityType = capacityType;
        MaxCapacity = maxCapacity;
        Description = NormalizeOptional(description);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
