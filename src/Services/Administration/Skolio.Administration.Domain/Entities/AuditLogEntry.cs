using Skolio.Administration.Domain.Exceptions;

namespace Skolio.Administration.Domain.Entities;

public sealed class AuditLogEntry
{
    private AuditLogEntry(Guid id, Guid actorUserId, string actionCode, string payload, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ActorUserId = actorUserId;
        ActionCode = actionCode.Trim();
        Payload = payload.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public Guid ActorUserId { get; }
    public string ActionCode { get; }
    public string Payload { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static AuditLogEntry Create(Guid id, Guid actorUserId, string actionCode, string payload, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || actorUserId == Guid.Empty)
            throw new AdministrationDomainException("Audit log ids are required.");
        if (string.IsNullOrWhiteSpace(actionCode))
            throw new AdministrationDomainException("Audit action code is required.");

        return new AuditLogEntry(id, actorUserId, actionCode, payload, createdAtUtc);
    }
}
