using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParticipantUserIds)
            .HasColumnName("participant_user_ids")
            .HasConversion(
                ids => string.Join(';', ids),
                value => value.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray())
            .HasMaxLength(2000)
            .IsRequired();
    }
}
