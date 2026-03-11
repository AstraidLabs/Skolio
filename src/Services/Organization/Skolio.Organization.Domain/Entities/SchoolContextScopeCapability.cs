using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeCapability
{
    private SchoolContextScopeCapability()
    {
    }

    private SchoolContextScopeCapability(
        Guid id,
        Guid matrixId,
        ScopeCapabilityCode capabilityCode,
        string translationKey,
        bool isEnabled)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope capability id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope capability matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope capability translation key is required.");
        }

        Id = id;
        MatrixId = matrixId;
        CapabilityCode = capabilityCode;
        TranslationKey = translationKey.Trim();
        IsEnabled = isEnabled;
    }

    public Guid Id { get; private set; }
    public Guid MatrixId { get; private set; }
    public ScopeCapabilityCode CapabilityCode { get; private set; }
    public string TranslationKey { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolContextScopeCapability Create(
        Guid id,
        Guid matrixId,
        ScopeCapabilityCode capabilityCode,
        string translationKey,
        bool isEnabled)
        => new(id, matrixId, capabilityCode, translationKey, isEnabled);
}
