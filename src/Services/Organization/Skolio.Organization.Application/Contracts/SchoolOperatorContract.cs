using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolOperatorContract(
    Guid Id,
    string LegalEntityName,
    LegalForm LegalForm,
    string? CompanyNumberIco,
    string? RedIzo,
    AddressContract RegisteredOfficeAddress,
    string? OperatorEmail,
    string? DataBox,
    string? ResortIdentifier,
    string? DirectorSummary,
    string? StatutoryBodySummary);
