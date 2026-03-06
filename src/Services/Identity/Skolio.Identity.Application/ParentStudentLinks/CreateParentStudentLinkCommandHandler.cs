using Mapster;
using MediatR;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Application.ParentStudentLinks;

public sealed class CreateParentStudentLinkCommandHandler(IIdentityCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateParentStudentLinkCommand, ParentStudentLinkContract>
{
    public async Task<ParentStudentLinkContract> Handle(CreateParentStudentLinkCommand request, CancellationToken cancellationToken)
    {
        var link = ParentStudentLink.Create(Guid.NewGuid(), request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship);
        await commandStore.AddParentStudentLinkAsync(link, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return link.Adapt<ParentStudentLinkContract>(mapsterConfig);
    }
}
