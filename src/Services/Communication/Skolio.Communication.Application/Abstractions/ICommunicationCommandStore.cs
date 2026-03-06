using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Abstractions;

public interface ICommunicationCommandStore
{
    Task AddAnnouncementAsync(Announcement announcement, CancellationToken cancellationToken);
    Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken);
    Task AddConversationMessageAsync(ConversationMessage message, CancellationToken cancellationToken);
    Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
