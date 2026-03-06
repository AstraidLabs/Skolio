using Skolio.Communication.Domain.Exceptions;

namespace Skolio.Communication.Domain.Entities;

public sealed class Conversation
{
    private Conversation(Guid id, Guid schoolId, string topic, IReadOnlyCollection<Guid> participantUserIds)
    {
        Id = id;
        SchoolId = schoolId;
        Topic = topic.Trim();
        ParticipantUserIds = participantUserIds;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public string Topic { get; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; }

    public static Conversation Create(Guid id, Guid schoolId, string topic, IReadOnlyCollection<Guid> participantUserIds)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
            throw new CommunicationDomainException("Conversation ids are required.");
        if (participantUserIds.Count < 2)
            throw new CommunicationDomainException("Conversation must include at least two participants.");

        return new Conversation(id, schoolId, topic, participantUserIds.Distinct().ToArray());
    }
}
