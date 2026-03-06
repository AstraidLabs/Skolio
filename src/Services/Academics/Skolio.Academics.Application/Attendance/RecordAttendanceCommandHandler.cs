using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Attendance;

public sealed class RecordAttendanceCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<RecordAttendanceCommand, AttendanceRecordContract>
{
    public async Task<AttendanceRecordContract> Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
    {
        var record = AttendanceRecord.Create(Guid.NewGuid(), request.SchoolId, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status);
        await commandStore.AddAttendanceRecordAsync(record, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return record.Adapt<AttendanceRecordContract>(mapsterConfig);
    }
}
