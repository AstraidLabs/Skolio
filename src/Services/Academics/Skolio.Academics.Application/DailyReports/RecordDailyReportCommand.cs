using MediatR;
using Skolio.Academics.Application.Contracts;

namespace Skolio.Academics.Application.DailyReports;

public sealed record RecordDailyReportCommand(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes) : IRequest<DailyReportContract>;
