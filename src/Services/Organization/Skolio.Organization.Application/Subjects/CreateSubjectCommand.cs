using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.Subjects;

public sealed record CreateSubjectCommand(Guid SchoolId, string Code, string Name) : IRequest<SubjectContract>;
