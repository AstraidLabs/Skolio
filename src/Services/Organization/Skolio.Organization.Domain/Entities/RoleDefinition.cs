using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// System-level role definition. Seeded as structural data.
/// Represents role metadata, not user assignments.
/// </summary>
public sealed class RoleDefinition
{
    private RoleDefinition()
    {
    }

    private RoleDefinition(
        Guid id,
        string roleCode,
        string translationKey,
        RoleScopeType scopeType,
        bool isBootstrapAllowed,
        bool isCreateUserFlowAllowed,
        bool isUserManagementAllowed,
        int sortOrder)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Role definition id is required.");
        }

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            throw new OrganizationDomainException("Role definition code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Role definition translation key is required.");
        }

        Id = id;
        RoleCode = roleCode.Trim();
        TranslationKey = translationKey.Trim();
        ScopeType = scopeType;
        IsBootstrapAllowed = isBootstrapAllowed;
        IsCreateUserFlowAllowed = isCreateUserFlowAllowed;
        IsUserManagementAllowed = isUserManagementAllowed;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public RoleScopeType ScopeType { get; private set; }
    public bool IsBootstrapAllowed { get; private set; }
    public bool IsCreateUserFlowAllowed { get; private set; }
    public bool IsUserManagementAllowed { get; private set; }
    public int SortOrder { get; private set; }

    public static RoleDefinition Create(
        Guid id,
        string roleCode,
        string translationKey,
        RoleScopeType scopeType,
        bool isBootstrapAllowed,
        bool isCreateUserFlowAllowed,
        bool isUserManagementAllowed,
        int sortOrder)
        => new(id, roleCode, translationKey, scopeType, isBootstrapAllowed, isCreateUserFlowAllowed, isUserManagementAllowed, sortOrder);
}
