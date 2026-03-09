using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolOperatorContract(
    Guid Id,
    string LegalEntityName,
    LegalForm LegalForm,
    string? CompanyNumberIco,
    AddressContract RegisteredOfficeAddress,
    string? ResortIdentifier,
    string? DirectorSummary,
    string? StatutoryBodySummary);
