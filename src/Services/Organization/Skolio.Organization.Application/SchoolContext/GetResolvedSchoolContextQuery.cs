using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.SchoolContext;

public sealed record GetResolvedSchoolContextQuery(Guid SchoolId) : IRequest<ResolvedSchoolContextContract?>;
