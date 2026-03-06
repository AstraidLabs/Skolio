using MediatR;
using Skolio.Administration.Application.Contracts;

namespace Skolio.Administration.Application.AuditLogs;

public sealed record WriteAuditLogEntryCommand(Guid ActorUserId, string ActionCode, string Payload) : IRequest<AuditLogEntryContract>;
