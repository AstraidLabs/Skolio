using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class Subject
{
    private Subject(Guid id, Guid schoolId, string code, string name)
    {
        Id = id;
        SchoolId = schoolId;
        SetCode(code);
        SetName(name);
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public static Subject Create(Guid id, Guid schoolId, string code, string name)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("Subject id and school id are required.");
        }

        return new Subject(id, schoolId, code, name);
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Subject code is required.");
        }

        Code = code.Trim();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("Subject name is required.");
        }

        Name = name.Trim();
    }
}
