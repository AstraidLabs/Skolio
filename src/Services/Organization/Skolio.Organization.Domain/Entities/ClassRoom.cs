using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class ClassRoom
{
    private ClassRoom(Guid id, Guid schoolId, Guid gradeLevelId, string code, string displayName)
    {
        Id = id;
        SchoolId = schoolId;
        GradeLevelId = gradeLevelId;
        SetCode(code);
        SetDisplayName(displayName);
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid GradeLevelId { get; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public static ClassRoom Create(Guid id, Guid schoolId, Guid gradeLevelId, SchoolType schoolType, string code, string displayName)
    {
        if (schoolType == SchoolType.Kindergarten)
        {
            throw new OrganizationDomainException("Kindergarten organization is group-first and does not use class rooms as primary structure.");
        }

        if (id == Guid.Empty || schoolId == Guid.Empty || gradeLevelId == Guid.Empty)
        {
            throw new OrganizationDomainException("Class room id, school id and grade level id are required.");
        }

        return new ClassRoom(id, schoolId, gradeLevelId, code, displayName);
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Class room code is required.");
        }

        Code = code.Trim();
    }

    private void SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new OrganizationDomainException("Class room display name is required.");
        }

        DisplayName = displayName.Trim();
    }
}
