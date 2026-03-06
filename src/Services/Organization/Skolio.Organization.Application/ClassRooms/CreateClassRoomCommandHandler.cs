using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Application.ClassRooms;

public sealed class CreateClassRoomCommandHandler(IOrganizationCommandStore commandStore, IOrganizationReadStore readStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateClassRoomCommand, ClassRoomContract>
{
    public async Task<ClassRoomContract> Handle(CreateClassRoomCommand request, CancellationToken cancellationToken)
    {
        var school = await readStore.GetSchoolAsync(request.SchoolId, cancellationToken)
            ?? throw new OrganizationDomainException("School was not found.");

        var classRoom = ClassRoom.Create(Guid.NewGuid(), request.SchoolId, request.GradeLevelId, school.SchoolType, request.Code, request.DisplayName);
        await commandStore.AddClassRoomAsync(classRoom, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return classRoom.Adapt<ClassRoomContract>(mapsterConfig);
    }
}
