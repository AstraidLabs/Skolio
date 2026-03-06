using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.Schools;

public sealed class CreateSchoolCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateSchoolCommand, SchoolContract>
{
    public async Task<SchoolContract> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        var school = School.Create(Guid.NewGuid(), request.Name, request.SchoolType);
        await commandStore.AddSchoolAsync(school, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return school.Adapt<SchoolContract>(mapsterConfig);
    }
}
