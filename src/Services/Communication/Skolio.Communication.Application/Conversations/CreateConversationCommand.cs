using MediatR;
using Skolio.Communication.Application.Contracts;

namespace Skolio.Communication.Application.Conversations;

public sealed record CreateConversationCommand(Guid SchoolId, string Topic, IReadOnlyCollection<Guid> ParticipantUserIds) : IRequest<ConversationContract>;
