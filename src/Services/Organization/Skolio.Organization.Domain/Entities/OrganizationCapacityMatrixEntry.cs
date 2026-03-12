using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Child matrix: defines which capacity types are tracked per school type scope.
/// Linked to the root SchoolContextScopeMatrix.
/// </summary>
public sealed class OrganizationCapacityMatrixEntry
{
    private OrganizationCapacityMatrixEntry()
    {
    }

    private OrganizationCapacityMatrixEntry(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        SchoolCapacityType capacityType,
        bool isRequired)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization capacity matrix entry id is required.");
        }

        if (parentScopeMatrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization capacity matrix parent scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Organization capacity matrix entry code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Organization capacity matrix entry translation key is required.");
        }

        Id = id;
        ParentScopeMatrixId = parentScopeMatrixId;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        CapacityType = capacityType;
        IsRequired = isRequired;
    }

    public Guid Id { get; private set; }
    public Guid ParentScopeMatrixId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public SchoolCapacityType CapacityType { get; private set; }
    public bool IsRequired { get; private set; }

    public SchoolContextScopeMatrix ParentScopeMatrix { get; private set; } = null!;

    public static OrganizationCapacityMatrixEntry Create(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        SchoolCapacityType capacityType,
        bool isRequired)
        => new(id, parentScopeMatrixId, code, translationKey, capacityType, isRequired);
}
