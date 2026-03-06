using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.Subjects;

public sealed class CreateSubjectCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateSubjectCommand, SubjectContract>
{
    public async Task<SubjectContract> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = Subject.Create(Guid.NewGuid(), request.SchoolId, request.Code, request.Name);
        await commandStore.AddSubjectAsync(subject, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return subject.Adapt<SubjectContract>(mapsterConfig);
    }
}
