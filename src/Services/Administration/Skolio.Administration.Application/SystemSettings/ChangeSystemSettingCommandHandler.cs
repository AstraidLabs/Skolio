using Mapster;
using MediatR;
using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.SystemSettings;

public sealed class ChangeSystemSettingCommandHandler(IAdministrationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<ChangeSystemSettingCommand, SystemSettingContract>
{
    public async Task<SystemSettingContract> Handle(ChangeSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = SystemSetting.Create(Guid.NewGuid(), request.Key, request.Value, request.IsSensitive);
        await commandStore.UpsertSystemSettingAsync(setting, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return setting.Adapt<SystemSettingContract>(mapsterConfig);
    }
}
