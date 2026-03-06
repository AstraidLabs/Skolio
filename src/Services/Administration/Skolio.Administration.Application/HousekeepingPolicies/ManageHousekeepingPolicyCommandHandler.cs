using Mapster;
using MediatR;
using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.HousekeepingPolicies;

public sealed class ManageHousekeepingPolicyCommandHandler(IAdministrationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<ManageHousekeepingPolicyCommand, HousekeepingPolicyContract>
{
    public async Task<HousekeepingPolicyContract> Handle(ManageHousekeepingPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = HousekeepingPolicy.Create(Guid.NewGuid(), request.PolicyName, request.RetentionDays);
        if (request.Activate) policy.Activate();
        await commandStore.UpsertHousekeepingPolicyAsync(policy, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return policy.Adapt<HousekeepingPolicyContract>(mapsterConfig);
    }
}
