using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolYearConfiguration : IEntityTypeConfiguration<SchoolYear>
{
    public void Configure(EntityTypeBuilder<SchoolYear> builder)
    {
        builder.ToTable("school_years");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(64).IsRequired();

        builder.OwnsOne(x => x.Period, owned =>
        {
            owned.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
            owned.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        });
    }
}
