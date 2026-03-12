using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.SchoolPlaces;

public sealed record CreateSchoolPlaceOfEducationCommand(
    Guid SchoolId,
    string Name,
    AddressContract Address,
    string? Description,
    bool IsPrimary) : IRequest<SchoolPlaceOfEducationContract>;
