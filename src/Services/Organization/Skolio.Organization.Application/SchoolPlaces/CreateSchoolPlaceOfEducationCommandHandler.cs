using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Application.SchoolPlaces;

public sealed class CreateSchoolPlaceOfEducationCommandHandler(IOrganizationCommandStore commandStore)
    : IRequestHandler<CreateSchoolPlaceOfEducationCommand, SchoolPlaceOfEducationContract>
{
    public async Task<SchoolPlaceOfEducationContract> Handle(
        CreateSchoolPlaceOfEducationCommand request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var address = Address.Create(request.Address.Street, request.Address.City, request.Address.PostalCode, request.Address.Country);
        var place = SchoolPlaceOfEducation.Create(id, request.SchoolId, request.Name, address, request.Description, request.IsPrimary);
        await commandStore.AddSchoolPlaceOfEducationAsync(place, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return new SchoolPlaceOfEducationContract(
            place.Id, place.SchoolId, place.Name,
            new AddressContract(place.Address.Street, place.Address.City, place.Address.PostalCode, place.Address.Country),
            place.Description, place.IsPrimary);
    }
}
