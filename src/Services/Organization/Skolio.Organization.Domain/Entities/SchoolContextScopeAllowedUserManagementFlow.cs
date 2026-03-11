using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeAllowedUserManagementFlow
{
    private SchoolContextScopeAllowedUserManagementFlow()
    {
    }

    private SchoolContextScopeAllowedUserManagementFlow(
        Guid id,
        Guid matrixId,
        UserManagementFlowCode flowCode,
        string translationKey)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed user management flow id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed user management flow matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope allowed user management flow translation key is required.");
        }

        Id = id;
        MatrixId = matrixId;
        FlowCode = flowCode;
        TranslationKey = translationKey.Trim();
    }

    public Guid Id { get; private set; }
    public Guid MatrixId { get; private set; }
    public UserManagementFlowCode FlowCode { get; private set; }
    public string TranslationKey { get; private set; } = string.Empty;

    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolContextScopeAllowedUserManagementFlow Create(
        Guid id,
        Guid matrixId,
        UserManagementFlowCode flowCode,
        string translationKey)
        => new(id, matrixId, flowCode, translationKey);
}
