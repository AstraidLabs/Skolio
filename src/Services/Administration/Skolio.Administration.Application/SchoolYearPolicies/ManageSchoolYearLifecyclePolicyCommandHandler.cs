using Mapster;
using MediatR;
using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.SchoolYearPolicies;

public sealed class ManageSchoolYearLifecyclePolicyCommandHandler(IAdministrationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<ManageSchoolYearLifecyclePolicyCommand, SchoolYearLifecyclePolicyContract>
{
    public async Task<SchoolYearLifecyclePolicyContract> Handle(ManageSchoolYearLifecyclePolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = SchoolYearLifecyclePolicy.Create(Guid.NewGuid(), request.SchoolId, request.PolicyName, request.ClosureGraceDays);
        if (request.Activate) policy.Activate();
        await commandStore.UpsertSchoolYearLifecyclePolicyAsync(policy, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return policy.Adapt<SchoolYearLifecyclePolicyContract>(mapsterConfig);
    }
}
