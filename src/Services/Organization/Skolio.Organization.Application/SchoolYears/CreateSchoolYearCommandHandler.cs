using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.SchoolYears;

public sealed class CreateSchoolYearCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateSchoolYearCommand, SchoolYearContract>
{
    public async Task<SchoolYearContract> Handle(CreateSchoolYearCommand request, CancellationToken cancellationToken)
    {
        var schoolYear = SchoolYear.Create(Guid.NewGuid(), request.SchoolId, request.Label, request.StartDate, request.EndDate);
        await commandStore.AddSchoolYearAsync(schoolYear, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return schoolYear.Adapt<SchoolYearContract>(mapsterConfig);
    }
}
