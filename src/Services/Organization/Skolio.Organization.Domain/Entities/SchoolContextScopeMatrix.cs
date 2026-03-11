using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolContextScopeMatrix
{
    private SchoolContextScopeMatrix()
    {
    }

    private SchoolContextScopeMatrix(
        Guid id,
        SchoolType schoolType,
        string code,
        string translationKey,
        string? description)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School context scope matrix id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("School context scope matrix code is required.");
        }

        if (string.IsNullOrWhiteSpace(translationKey))
        {
            throw new OrganizationDomainException("School context scope matrix translation key is required.");
        }

        Id = id;
        SchoolType = schoolType;
        Code = code.Trim();
        TranslationKey = translationKey.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public Guid Id { get; private set; }
    public SchoolType SchoolType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TranslationKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public IReadOnlyCollection<SchoolContextScopeCapability> Capabilities { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedRole> AllowedRoles { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedProfileSection> AllowedProfileSections { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedCreateUserFlow> AllowedCreateUserFlows { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedUserManagementFlow> AllowedUserManagementFlows { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedOrganizationSection> AllowedOrganizationSections { get; private set; } = [];
    public IReadOnlyCollection<SchoolContextScopeAllowedAcademicsSection> AllowedAcademicsSections { get; private set; } = [];

    public static SchoolContextScopeMatrix Create(
        Guid id,
        SchoolType schoolType,
        string code,
        string translationKey,
        string? description = null)
        => new(id, schoolType, code, translationKey, description);
}
