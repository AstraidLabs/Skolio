using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SecondaryFieldOfStudyConfiguration : IEntityTypeConfiguration<SecondaryFieldOfStudy>
{
    public void Configure(EntityTypeBuilder<SecondaryFieldOfStudy> builder)
    {
        builder.ToTable("secondary_fields_of_study");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
    }
}
