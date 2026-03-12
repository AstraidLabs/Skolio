using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Child matrix: defines which academic structural elements are relevant per school type scope.
/// Linked to the root SchoolContextScopeMatrix.
/// Controls: subjects, fields of study availability.
/// </summary>
public sealed class OrganizationAcademicStructureMatrixEntry
{
    private OrganizationAcademicStructureMatrixEntry()
    {
    }

    private OrganizationAcademicStructureMatrixEntry(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool usesSubjects,
        bool usesFieldOfStudy,
        bool subjectIsClassBound,
        bool fieldOfStudyIsRequired)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization academic structure matrix entry id is required.");
        }

        if (parentScopeMatrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("Organization academic structure matrix parent scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Organization academic structure matrix entry code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("Organization academic structure matrix entry translation key is required.");
        }

        Id = id;
        ParentScopeMatrixId = parentScopeMatrixId;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        UsesSubjects = usesSubjects;
        UsesFieldOfStudy = usesFieldOfStudy;
        SubjectIsClassBound = subjectIsClassBound;
        FieldOfStudyIsRequired = fieldOfStudyIsRequired;
    }

    public Guid Id { get; private set; }
    public Guid ParentScopeMatrixId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public bool UsesSubjects { get; private set; }
    public bool UsesFieldOfStudy { get; private set; }
    public bool SubjectIsClassBound { get; private set; }
    public bool FieldOfStudyIsRequired { get; private set; }

    public SchoolContextScopeMatrix ParentScopeMatrix { get; private set; } = null!;

    public static OrganizationAcademicStructureMatrixEntry Create(
        Guid id,
        Guid parentScopeMatrixId,
        string code,
        string translationKey,
        bool usesSubjects,
        bool usesFieldOfStudy,
        bool subjectIsClassBound,
        bool fieldOfStudyIsRequired)
        => new(id, parentScopeMatrixId, code, translationKey, usesSubjects, usesFieldOfStudy, subjectIsClassBound, fieldOfStudyIsRequired);
}
