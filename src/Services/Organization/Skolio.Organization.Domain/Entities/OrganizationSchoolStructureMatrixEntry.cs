using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Child matrix: defines which structural elements are available for a given school type scope.
/// Linked to the root SchoolContextScopeMatrix. Cannot contradict root capabilities.
/// Controls: GradeLevels, Classes, Groups availability per school type.
/// </summary>
public sealed class OrganizationSchoolStructureMatrixEntry
{
    private OrganizationSchoolStructureMatrixEntry()
    {
    }

    private OrganizationSchoolStructureMatrixEntry(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool usesGradeLevels,
        bool usesClasses,
        bool usesGroups,
        bool groupIsPrimaryStructure)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization school structure matrix entry id is required.");
        }

        if (parentScopeMatrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization school structure matrix parent scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Organization school structure matrix entry code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Organization school structure matrix entry translation key is required.");
        }

        Id = id;
        ParentScopeMatrixId = parentScopeMatrixId;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        UsesGradeLevels = usesGradeLevels;
        UsesClasses = usesClasses;
        UsesGroups = usesGroups;
        GroupIsPrimaryStructure = groupIsPrimaryStructure;
    }

    public Guid Id { get; private set; }
    public Guid ParentScopeMatrixId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public bool UsesGradeLevels { get; private set; }
    public bool UsesClasses { get; private set; }
    public bool UsesGroups { get; private set; }
    public bool GroupIsPrimaryStructure { get; private set; }

    public SchoolContextScopeMatrix ParentScopeMatrix { get; private set; } = null!;

    public static OrganizationSchoolStructureMatrixEntry Create(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool usesGradeLevels,
        bool usesClasses,
        bool usesGroups,
        bool groupIsPrimaryStructure)
        => new(id, parentScopeMatrixId, code, translationKey, usesGradeLevels, usesClasses, usesGroups, groupIsPrimaryStructure);
}
