using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Child matrix: defines which assignment patterns are available per school type scope.
/// Linked to the root SchoolContextScopeMatrix.
/// Controls: teacher assignment scope options, student placement patterns.
/// </summary>
public sealed class OrganizationAssignmentMatrixEntry
{
    private OrganizationAssignmentMatrixEntry()
    {
    }

    private OrganizationAssignmentMatrixEntry(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool allowsClassRoomAssignment,
        bool allowsGroupAssignment,
        bool allowsSubjectAssignment,
        bool studentRequiresClassPlacement,
        bool studentRequiresGroupPlacement)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization assignment matrix entry id is required.");
        }

        if (parentScopeMatrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization assignment matrix parent scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Organization assignment matrix entry code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Organization assignment matrix entry translation key is required.");
        }

        Id = id;
        ParentScopeMatrixId = parentScopeMatrixId;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        AllowsClassRoomAssignment = allowsClassRoomAssignment;
        AllowsGroupAssignment = allowsGroupAssignment;
        AllowsSubjectAssignment = allowsSubjectAssignment;
        StudentRequiresClassPlacement = studentRequiresClassPlacement;
        StudentRequiresGroupPlacement = studentRequiresGroupPlacement;
    }

    public Guid Id { get; private set; }
    public Guid ParentScopeMatrixId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public bool AllowsClassRoomAssignment { get; private set; }
    public bool AllowsGroupAssignment { get; private set; }
    public bool AllowsSubjectAssignment { get; private set; }
    public bool StudentRequiresClassPlacement { get; private set; }
    public bool StudentRequiresGroupPlacement { get; private set; }

    public SchoolContextScopeMatrix ParentScopeMatrix { get; private set; } = null!;

    public static OrganizationAssignmentMatrixEntry Create(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool allowsClassRoomAssignment,
        bool allowsGroupAssignment,
        bool allowsSubjectAssignment,
        bool studentRequiresClassPlacement,
        bool studentRequiresGroupPlacement)
        => new(id, parentScopeMatrixId, code, translationKey, allowsClassRoomAssignment, allowsGroupAssignment, allowsSubjectAssignment, studentRequiresClassPlacement, studentRequiresGroupPlacement);
}
