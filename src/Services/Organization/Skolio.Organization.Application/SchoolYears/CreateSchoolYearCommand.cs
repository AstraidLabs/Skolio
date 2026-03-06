using MediatR;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Application.SchoolYears;

public sealed record CreateSchoolYearCommand(Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate) : IRequest<SchoolYearContract>;
