using MediatR;
using Skolio.Communication.Application.Contracts;

namespace Skolio.Communication.Application.Conversations;

public sealed record AddConversationMessageCommand(Guid ConversationId, Guid SenderUserId, string Message, DateTimeOffset SentAtUtc) : IRequest<ConversationMessageContract>;
