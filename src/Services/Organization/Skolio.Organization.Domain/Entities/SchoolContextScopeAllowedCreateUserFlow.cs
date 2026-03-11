using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeAllowedCreateUserFlow
{
    private SchoolContextScopeAllowedCreateUserFlow()
    {
    }

    private SchoolContextScopeAllowedCreateUserFlow(
        Guid id,
        Guid matrixId,
        CreateUserFlowCode flowCode,
        string translationKey)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed create user flow id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed create user flow matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope allowed create user flow translation key is required.");
        }

        Id = id;
        MatrixId = matrixId;
        FlowCode = flowCode;
        TranslationKey = translationKey.Trim();
    }

    public Guid Id { get; private set; }
    public Guid MatrixId { get; private set; }
    public CreateUserFlowCode FlowCode { get; private set; }
    public string TranslationKey { get; private set; } = string.Empty;

    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolContextScopeAllowedCreateUserFlow Create(
        Guid id,
        Guid matrixId,
        CreateUserFlowCode flowCode,
        string translationKey)
        => new(id, matrixId, flowCode, translationKey);
}
