using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.Property(x => x.ActionCode).HasColumnName("action_code").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.ActionCode);
    }
}
