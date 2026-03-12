using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Child matrix: defines which registry and identity data fields are relevant per school type scope.
/// Linked to the root SchoolContextScopeMatrix. Cannot contradict root.
/// Controls: which legal/registry sections are visible and editable.
/// </summary>
public sealed class OrganizationRegistryMatrixEntry
{
    private OrganizationRegistryMatrixEntry()
    {
    }

    private OrganizationRegistryMatrixEntry(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool requiresIzo,
        bool requiresRedIzo,
        bool requiresIco,
        bool requiresDataBox,
        bool requiresFounder,
        bool requiresTeachingLanguage)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization registry matrix entry id is required.");
        }

        if (parentScopeMatrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization registry matrix parent scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Organization registry matrix entry code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Organization registry matrix entry translation key is required.");
        }

        Id = id;
        ParentScopeMatrixId = parentScopeMatrixId;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        RequiresIzo = requiresIzo;
        RequiresRedIzo = requiresRedIzo;
        RequiresIco = requiresIco;
        RequiresDataBox = requiresDataBox;
        RequiresFounder = requiresFounder;
        RequiresTeachingLanguage = requiresTeachingLanguage;
    }

    public Guid Id { get; private set; }
    public Guid ParentScopeMatrixId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public bool RequiresIzo { get; private set; }
    public bool RequiresRedIzo { get; private set; }
    public bool RequiresIco { get; private set; }
    public bool RequiresDataBox { get; private set; }
    public bool RequiresFounder { get; private set; }
    public bool RequiresTeachingLanguage { get; private set; }

    public SchoolContextScopeMatrix ParentScopeMatrix { get; private set; } = null!;

    public static OrganizationRegistryMatrixEntry Create(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool requiresIzo,
        bool requiresRedIzo,
        bool requiresIco,
        bool requiresDataBox,
        bool requiresFounder,
        bool requiresTeachingLanguage)
        => new(id, parentScopeMatrixId, code, translationKey, requiresIzo, requiresRedIzo, requiresIco, requiresDataBox, requiresFounder, requiresTeachingLanguage);
}
