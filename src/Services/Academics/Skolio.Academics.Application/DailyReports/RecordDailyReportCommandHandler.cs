using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.DailyReports;

public sealed class RecordDailyReportCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<RecordDailyReportCommand, DailyReportContract>
{
    public async Task<DailyReportContract> Handle(RecordDailyReportCommand request, CancellationToken cancellationToken)
    {
        var report = DailyReport.Create(Guid.NewGuid(), request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes);
        await commandStore.AddDailyReportAsync(report, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return report.Adapt<DailyReportContract>(mapsterConfig);
    }
}
