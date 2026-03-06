using Mapster;
using MediatR;
using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.AuditLogs;

public sealed class WriteAuditLogEntryCommandHandler(IAdministrationCommandStore commandStore, IAdministrationClock clock, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<WriteAuditLogEntryCommand, AuditLogEntryContract>
{
    public async Task<AuditLogEntryContract> Handle(WriteAuditLogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = AuditLogEntry.Create(Guid.NewGuid(), request.ActorUserId, request.ActionCode, request.Payload, clock.UtcNow);
        await commandStore.AddAuditLogEntryAsync(entry, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return entry.Adapt<AuditLogEntryContract>(mapsterConfig);
    }
}
