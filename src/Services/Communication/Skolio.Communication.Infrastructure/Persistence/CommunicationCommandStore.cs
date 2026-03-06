using Microsoft.Extensions.Caching.Distributed;
using Skolio.Communication.Application.Abstractions;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Infrastructure.Persistence;

public sealed class CommunicationCommandStore(CommunicationDbContext dbContext, IDistributedCache distributedCache) : ICommunicationCommandStore
{
    public Task AddAnnouncementAsync(Announcement announcement, CancellationToken cancellationToken) => dbContext.Announcements.AddAsync(announcement, cancellationToken).AsTask();
    public Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken) => dbContext.Conversations.AddAsync(conversation, cancellationToken).AsTask();
    public Task AddConversationMessageAsync(ConversationMessage message, CancellationToken cancellationToken) => dbContext.ConversationMessages.AddAsync(message, cancellationToken).AsTask();
    public async Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        await dbContext.Notifications.AddAsync(notification, cancellationToken);
        await distributedCache.SetStringAsync($"communication:notification:{notification.Id}", "queued", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) }, cancellationToken);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
