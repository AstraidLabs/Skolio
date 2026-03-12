using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record RoleDefinitionContract(
    Guid Id,
    string RoleCode,
    string TranslationKey,
    RoleScopeType ScopeType,
    bool IsBootstrapAllowed,
    bool IsCreateUserFlowAllowed,
    bool IsUserManagementAllowed,
    int SortOrder);
