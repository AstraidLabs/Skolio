using MediatR;
using Skolio.Academics.Application.Contracts;

namespace Skolio.Academics.Application.Homework;

public sealed record AssignHomeworkCommand(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate) : IRequest<HomeworkAssignmentContract>;
