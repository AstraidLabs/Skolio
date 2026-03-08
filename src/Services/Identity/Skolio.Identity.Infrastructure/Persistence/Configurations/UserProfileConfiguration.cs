using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.UserType).HasColumnName("user_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreferredDisplayName).HasColumnName("preferred_display_name").HasMaxLength(120);
        builder.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(16);
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
        builder.Property(x => x.PositionTitle).HasColumnName("position_title").HasMaxLength(120);
        builder.Property(x => x.PublicContactNote).HasColumnName("public_contact_note").HasMaxLength(240);
        builder.Property(x => x.PreferredContactNote).HasColumnName("preferred_contact_note").HasMaxLength(240);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }
}
