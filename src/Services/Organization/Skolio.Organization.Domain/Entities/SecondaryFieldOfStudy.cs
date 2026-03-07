using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SecondaryFieldOfStudy
{
    private SecondaryFieldOfStudy(Guid id, Guid schoolId, string code, string name)
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

    public static SecondaryFieldOfStudy Create(Guid id, Guid schoolId, SchoolType schoolType, string code, string name)
    {
        if (schoolType != SchoolType.SecondarySchool)
        {
            throw new OrganizationDomainException("Field of study is available only for secondary schools.");
        }

        if (id == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("Field of study id and school id are required.");
        }

        return new SecondaryFieldOfStudy(id, schoolId, code, name);
    }

    public void Update(SchoolType schoolType, string code, string name)
    {
        if (schoolType != SchoolType.SecondarySchool)
        {
            throw new OrganizationDomainException("Field of study is available only for secondary schools.");
        }

        SetCode(code);
        SetName(name);
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Field of study code is required.");
        }

        Code = code.Trim();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("Field of study name is required.");
        }

        Name = name.Trim();
    }
}
