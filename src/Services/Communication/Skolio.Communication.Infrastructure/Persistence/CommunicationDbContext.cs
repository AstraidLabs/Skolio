using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Infrastructure.Persistence;

public sealed class CommunicationDbContext : DbContext
{
    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : base(options) { }

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunicationDbContext).Assembly);
}
