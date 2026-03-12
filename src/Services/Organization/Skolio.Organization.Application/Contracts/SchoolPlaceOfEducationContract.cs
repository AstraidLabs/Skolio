namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolPlaceOfEducationContract(
    Guid Id,
    Guid SchoolId,
    string Name,
    AddressContract Address,
    string? Description,
    bool IsPrimary);
