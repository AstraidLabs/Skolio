using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class DailyReport
{
    private DailyReport(Guid id, Guid schoolId, Guid audienceId, DateOnly reportDate, string summary, string notes)
    {
        Id = id;
        SchoolId = schoolId;
        AudienceId = audienceId;
        ReportDate = reportDate;
        Summary = summary.Trim();
        Notes = notes.Trim();
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid AudienceId { get; private set; }
    public DateOnly ReportDate { get; private set; }
    public string Summary { get; private set; }
    public string Notes { get; private set; }

    public static DailyReport Create(Guid id, Guid schoolId, Guid audienceId, DateOnly reportDate, string summary, string notes)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty || audienceId == Guid.Empty)
            throw new AcademicsDomainException("Daily report ids are required.");
        if (string.IsNullOrWhiteSpace(summary))
            throw new AcademicsDomainException("Daily report summary is required.");

        return new DailyReport(id, schoolId, audienceId, reportDate, summary, notes);
    }

    public void OverrideForPlatformSupport(Guid audienceId, DateOnly reportDate, string summary, string notes)
    {
        if (audienceId == Guid.Empty)
            throw new AcademicsDomainException("Daily report ids are required.");
        if (string.IsNullOrWhiteSpace(summary))
            throw new AcademicsDomainException("Daily report summary is required.");

        AudienceId = audienceId;
        ReportDate = reportDate;
        Summary = summary.Trim();
        Notes = notes.Trim();
    }
}