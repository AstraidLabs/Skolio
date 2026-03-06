using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.ToTable("daily_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.AudienceId).HasColumnName("audience_id").IsRequired();
        builder.Property(x => x.ReportDate).HasColumnName("report_date").IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(3000).IsRequired();
    }
}
