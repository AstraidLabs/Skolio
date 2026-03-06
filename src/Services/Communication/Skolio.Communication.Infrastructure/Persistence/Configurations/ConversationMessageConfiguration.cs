using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Infrastructure.Persistence.Configurations;

public sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("conversation_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasMaxLength(3000).IsRequired();
        builder.Property(x => x.SentAtUtc).HasColumnName("sent_at_utc").IsRequired();
    }
}
