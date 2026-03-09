using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record FounderContract(
    Guid Id,
    FounderType FounderType,
    FounderCategory FounderCategory,
    string FounderName,
    LegalForm FounderLegalForm,
    string? FounderIco,
    AddressContract FounderAddress,
    string? FounderEmail);
