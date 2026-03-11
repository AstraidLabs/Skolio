using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeAllowedAcademicsSection
{
    private SchoolContextScopeAllowedAcademicsSection()
    {
    }

    private SchoolContextScopeAllowedAcademicsSection(
        Guid id,
        Guid matrixId,
        AcademicsSectionCode sectionCode,
        string translationKey)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed academics section id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope allowed academics section matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope allowed academics section translation key is required.");
        }

        Id = id;
        MatrixId = matrixId;
        SectionCode = sectionCode;
        TranslationKey = translationKey.Trim();
    }

    public Guid Id { get; private set; }
    public Guid MatrixId { get; private set; }
    public AcademicsSectionCode SectionCode { get; private set; }
    public string TranslationKey { get; private set; } = string.Empty;

    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolContextScopeAllowedAcademicsSection Create(
        Guid id,
        Guid matrixId,
        AcademicsSectionCode sectionCode,
        string translationKey)
        => new(id, matrixId, sectionCode, translationKey);
}
