using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Domain.Entities;

public sealed class SchoolRoleAssignment
{
    private SchoolRoleAssignment(Guid id, Guid userProfileId, Guid schoolId, string roleCode)
    {
        Id = id;
        UserProfileId = userProfileId;
        SchoolId = schoolId;
        RoleCode = roleCode.Trim();
    }

    public Guid Id { get; }
    public Guid UserProfileId { get; }
    public Guid SchoolId { get; }
    public string RoleCode { get; }

    public static SchoolRoleAssignment Create(Guid id, Guid userProfileId, Guid schoolId, string roleCode)
    {
        if (id == Guid.Empty || userProfileId == Guid.Empty || schoolId == Guid.Empty)
            throw new IdentityDomainException("School role assignment ids are required.");
        if (string.IsNullOrWhiteSpace(roleCode))
            throw new IdentityDomainException("School role code is required.");

        return new SchoolRoleAssignment(id, userProfileId, schoolId, roleCode);
    }
}
