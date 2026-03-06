using MediatR;
using Skolio.Academics.Application.Contracts;

namespace Skolio.Academics.Application.Excuses;

public sealed record SubmitExcuseNoteCommand(Guid AttendanceRecordId, Guid ParentUserId, string Reason) : IRequest<ExcuseNoteContract>;
