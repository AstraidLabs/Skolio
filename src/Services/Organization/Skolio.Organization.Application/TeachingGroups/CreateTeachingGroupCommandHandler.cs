using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.TeachingGroups;

public sealed class CreateTeachingGroupCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateTeachingGroupCommand, TeachingGroupContract>
{
    public async Task<TeachingGroupContract> Handle(CreateTeachingGroupCommand request, CancellationToken cancellationToken)
    {
        var teachingGroup = TeachingGroup.Create(Guid.NewGuid(), request.SchoolId, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup);
        await commandStore.AddTeachingGroupAsync(teachingGroup, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return teachingGroup.Adapt<TeachingGroupContract>(mapsterConfig);
    }
}
