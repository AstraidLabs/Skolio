using MediatR;
using Skolio.Administration.Application.Contracts;

namespace Skolio.Administration.Application.HousekeepingPolicies;

public sealed record ManageHousekeepingPolicyCommand(string PolicyName, int RetentionDays, bool Activate) : IRequest<HousekeepingPolicyContract>;
