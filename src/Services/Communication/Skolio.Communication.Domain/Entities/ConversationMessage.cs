using Skolio.Communication.Domain.Exceptions;

namespace Skolio.Communication.Domain.Entities;

public sealed class ConversationMessage
{
    private ConversationMessage(Guid id, Guid conversationId, Guid senderUserId, string message, DateTimeOffset sentAtUtc)
    {
        Id = id;
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Message = message.Trim();
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; }
    public Guid ConversationId { get; }
    public Guid SenderUserId { get; }
    public string Message { get; }
    public DateTimeOffset SentAtUtc { get; }

    public static ConversationMessage Create(Guid id, Guid conversationId, Guid senderUserId, string message, DateTimeOffset sentAtUtc)
    {
        if (id == Guid.Empty || conversationId == Guid.Empty || senderUserId == Guid.Empty)
            throw new CommunicationDomainException("Conversation message ids are required.");
        if (string.IsNullOrWhiteSpace(message))
            throw new CommunicationDomainException("Conversation message content is required.");

        return new ConversationMessage(id, conversationId, senderUserId, message, sentAtUtc);
    }
}
