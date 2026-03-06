using MediatR;
using Skolio.Academics.Application.Contracts;

namespace Skolio.Academics.Application.Grades;

public sealed record RecordGradeEntryCommand(Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn) : IRequest<GradeEntryContract>;
