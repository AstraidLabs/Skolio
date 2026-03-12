using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.SchoolCapacities;

public sealed class CreateSchoolCapacityCommandHandler(IOrganizationCommandStore commandStore)
    : IRequestHandler<CreateSchoolCapacityCommand, SchoolCapacityContract>
{
    public async Task<SchoolCapacityContract> Handle(
        CreateSchoolCapacityCommand request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var capacity = SchoolCapacity.Create(id, request.SchoolId, request.CapacityType, request.MaxCapacity, request.Description);
        await commandStore.AddSchoolCapacityAsync(capacity, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return new SchoolCapacityContract(capacity.Id, capacity.SchoolId, capacity.CapacityType, capacity.MaxCapacity, capacity.Description);
    }
}
