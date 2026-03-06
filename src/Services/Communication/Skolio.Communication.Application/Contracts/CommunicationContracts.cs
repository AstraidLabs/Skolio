using Skolio.Communication.Domain.Enums;

namespace Skolio.Communication.Application.Contracts;

public sealed record AnnouncementContract(Guid Id, Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc);
public sealed record ConversationContract(Guid Id, Guid SchoolId, string Topic, IReadOnlyCollection<Guid> ParticipantUserIds);
public sealed record ConversationMessageContract(Guid Id, Guid ConversationId, Guid SenderUserId, string Message, DateTimeOffset SentAtUtc);
public sealed record NotificationContract(Guid Id, Guid RecipientUserId, string Title, string Body, NotificationChannel Channel);
