using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class TeachingGroup
{
    private TeachingGroup(Guid id, Guid schoolId, Guid? classRoomId, string name, bool isDailyOperationsGroup)
    {
        Id = id;
        SchoolId = schoolId;
        ClassRoomId = classRoomId;
        SetName(name);
        IsDailyOperationsGroup = isDailyOperationsGroup;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid? ClassRoomId { get; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDailyOperationsGroup { get; }

    public static TeachingGroup Create(Guid id, Guid schoolId, Guid? classRoomId, string name, bool isDailyOperationsGroup)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("Teaching group id and school id are required.");
        }

        return new TeachingGroup(id, schoolId, classRoomId, name, isDailyOperationsGroup);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("Teaching group name is required.");
        }

        Name = name.Trim();
    }
}
