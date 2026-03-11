using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Domain.Entities;

public sealed class SchoolOperator
{
    private SchoolOperator()
    {
    }

    private SchoolOperator(
        Guid id,
        string legalEntityName,
        LegalForm legalForm,
        string? companyNumberIco,
        string? redIzo,
        Address registeredOfficeAddress,
        string? operatorEmail,
        string? dataBox,
        string? resortIdentifier,
        string? directorSummary,
        string? statutoryBodySummary)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School operator id is required.");
        }

        Id = id;
        Update(legalEntityName, legalForm, companyNumberIco, redIzo, registeredOfficeAddress, operatorEmail, dataBox, resortIdentifier, directorSummary, statutoryBodySummary);
    }

    public Guid Id { get; private set; }
    public string LegalEntityName { get; private set; } = string.Empty;
    public LegalForm LegalForm { get; private set; }
    public string? CompanyNumberIco { get; private set; }
    public string? RedIzo { get; private set; }
    public Address RegisteredOfficeAddress { get; private set; } = Address.Create("Unknown", "Unknown", "00000", "CZ");
    public string? OperatorEmail { get; private set; }
    public string? DataBox { get; private set; }
    public string? ResortIdentifier { get; private set; }
    public string? DirectorSummary { get; private set; }
    public string? StatutoryBodySummary { get; private set; }

    public static SchoolOperator Create(
        Guid id,
        string legalEntityName,
        LegalForm legalForm,
        string? companyNumberIco,
        string? redIzo,
        Address registeredOfficeAddress,
        string? operatorEmail,
        string? dataBox,
        string? resortIdentifier,
        string? directorSummary,
        string? statutoryBodySummary)
        => new(id, legalEntityName, legalForm, companyNumberIco, redIzo, registeredOfficeAddress, operatorEmail, dataBox, resortIdentifier, directorSummary, statutoryBodySummary);

    public void Update(
        string legalEntityName,
        LegalForm legalForm,
        string? companyNumberIco,
        string? redIzo,
        Address registeredOfficeAddress,
        string? operatorEmail,
        string? dataBox,
        string? resortIdentifier,
        string? directorSummary,
        string? statutoryBodySummary)
    {
        if (string.IsNullOrWhiteSpace(legalEntityName))
        {
            throw new OrganizationDomainException("School operator legal entity name is required.");
        }

        LegalEntityName = legalEntityName.Trim();
        LegalForm = legalForm;
        CompanyNumberIco = NormalizeOptional(companyNumberIco);
        RedIzo = NormalizeOptional(redIzo);
        RegisteredOfficeAddress = registeredOfficeAddress;
        OperatorEmail = NormalizeOptional(operatorEmail);
        DataBox = NormalizeOptional(dataBox);
        ResortIdentifier = NormalizeOptional(resortIdentifier);
        DirectorSummary = NormalizeOptional(directorSummary);
        StatutoryBodySummary = NormalizeOptional(statutoryBodySummary);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
