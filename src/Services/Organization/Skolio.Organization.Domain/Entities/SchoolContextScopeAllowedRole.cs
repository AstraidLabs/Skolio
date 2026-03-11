using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeAllowedRole
{
    private SchoolContextScopeAllowedRole()
    {
    }

    private SchoolContextScopeAllowedRole(
        Guid id,
        Guid matrixId,
        string roleCode,
        string translationKey)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed role id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed role matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            throw new OrganizationDomainException("School context scope allowed role code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope allowed role translation key is required.");
        }

        Id = id;
        MatrixId = matrixId;
        RoleCode = roleCode.Trim();
        TranslationKey = translationKey.Trim();
    }

    public Guid Id { get; private set; }
    public Guid MatrixId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;

    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolContextScopeAllowedRole Create(
        Guid id,
        Guid matrixId,
        string roleCode,
        string translationKey)
        => new(id, matrixId, roleCode, translationKey);
}
