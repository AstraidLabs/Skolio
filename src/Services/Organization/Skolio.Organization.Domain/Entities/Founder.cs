using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Domain.Entities;

public sealed class Founder
{
    private Founder()
    {
    }

    private Founder(
        Guid id,
        FounderType founderType,
        FounderCategory founderCategory,
        string founderName,
        LegalForm founderLegalForm,
        string? founderIco,
        Address founderAddress,
        string? founderEmail)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("Founder id is required.");
        }

        Id = id;
        Update(founderType, founderCategory, founderName, founderLegalForm, founderIco, founderAddress, founderEmail);
    }

    public Guid Id { get; private set; }
    public FounderType FounderType { get; private set; }
    public FounderCategory FounderCategory { get; private set; }
    public string FounderName { get; private set; } = string.Empty;
    public LegalForm FounderLegalForm { get; private set; }
    public string? FounderIco { get; private set; }
    public Address FounderAddress { get; private set; } = Address.Create("Unknown", "Unknown", "00000", "CZ");
    public string? FounderEmail { get; private set; }

    public static Founder Create(
        Guid id,
        FounderType founderType,
        FounderCategory founderCategory,
        string founderName,
        LegalForm founderLegalForm,
        string? founderIco,
        Address founderAddress,
        string? founderEmail)
        => new(id, founderType, founderCategory, founderName, founderLegalForm, founderIco, founderAddress, founderEmail);

    public void Update(
        FounderType founderType,
        FounderCategory founderCategory,
        string founderName,
        LegalForm founderLegalForm,
        string? founderIco,
        Address founderAddress,
        string? founderEmail)
    {
        if (string.IsNullOrWhiteSpace(founderName))
        {
            throw new OrganizationDomainException("Founder name is required.");
        }

        FounderType = founderType;
        FounderCategory = founderCategory;
        FounderName = founderName.Trim();
        FounderLegalForm = founderLegalForm;
        FounderIco = NormalizeOptional(founderIco);
        FounderAddress = founderAddress;
        FounderEmail = NormalizeOptional(founderEmail);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
