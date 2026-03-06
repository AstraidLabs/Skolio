using Mapster;
using MediatR;
using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.FeatureToggles;

public sealed class ChangeFeatureToggleCommandHandler(IAdministrationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<ChangeFeatureToggleCommand, FeatureToggleContract>
{
    public async Task<FeatureToggleContract> Handle(ChangeFeatureToggleCommand request, CancellationToken cancellationToken)
    {
        var toggle = FeatureToggle.Create(Guid.NewGuid(), request.FeatureCode, request.IsEnabled);
        await commandStore.UpsertFeatureToggleAsync(toggle, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return toggle.Adapt<FeatureToggleContract>(mapsterConfig);
    }
}
