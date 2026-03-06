using MediatR;
using Skolio.Administration.Application.Contracts;

namespace Skolio.Administration.Application.SchoolYearPolicies;

public sealed record ManageSchoolYearLifecyclePolicyCommand(Guid SchoolId, string PolicyName, int ClosureGraceDays, bool Activate) : IRequest<SchoolYearLifecyclePolicyContract>;
