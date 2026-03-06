using Skolio.Communication.Domain.Enums;
using Skolio.Communication.Domain.Exceptions;

namespace Skolio.Communication.Domain.Entities;

public sealed class Notification
{
    private Notification(Guid id, Guid recipientUserId, string title, string body, NotificationChannel channel)
    {
        Id = id;
        RecipientUserId = recipientUserId;
        Title = title.Trim();
        Body = body.Trim();
        Channel = channel;
    }

    public Guid Id { get; }
    public Guid RecipientUserId { get; }
    public string Title { get; }
    public string Body { get; }
    public NotificationChannel Channel { get; }

    public static Notification Create(Guid id, Guid recipientUserId, string title, string body, NotificationChannel channel)
    {
        if (id == Guid.Empty || recipientUserId == Guid.Empty)
            throw new CommunicationDomainException("Notification ids are required.");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            throw new CommunicationDomainException("Notification title and body are required.");

        return new Notification(id, recipientUserId, title, body, channel);
    }
}
