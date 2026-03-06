using Skolio.Communication.Domain.Exceptions;

namespace Skolio.Communication.Domain.Entities;

public sealed class Announcement
{
    private Announcement(Guid id, Guid schoolId, string title, string message, DateTimeOffset publishAtUtc)
    {
        Id = id;
        SchoolId = schoolId;
        Title = title.Trim();
        Message = message.Trim();
        PublishAtUtc = publishAtUtc;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public string Title { get; }
    public string Message { get; }
    public DateTimeOffset PublishAtUtc { get; }

    public static Announcement Create(Guid id, Guid schoolId, string title, string message, DateTimeOffset publishAtUtc)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
            throw new CommunicationDomainException("Announcement ids are required.");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            throw new CommunicationDomainException("Announcement title and message are required.");

        return new Announcement(id, schoolId, title, message, publishAtUtc);
    }
}
