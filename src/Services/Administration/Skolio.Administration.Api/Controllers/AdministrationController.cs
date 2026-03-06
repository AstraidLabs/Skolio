using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Administration.Application.AuditLogs;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Application.FeatureToggles;
using Skolio.Administration.Application.HousekeepingPolicies;
using Skolio.Administration.Application.SchoolYearPolicies;
using Skolio.Administration.Application.SystemSettings;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.PlatformAdministration)]
[Route("api/administration")]
public sealed class AdministrationController(IMediator mediator) : ControllerBase
{
    [HttpPost("system-settings")]
    public async Task<ActionResult<SystemSettingContract>> ChangeSetting([FromBody] ChangeSystemSettingRequest request, CancellationToken cancellationToken)
        => Created(string.Empty, await mediator.Send(new ChangeSystemSettingCommand(request.Key, request.Value, request.IsSensitive), cancellationToken));

    [HttpPost("feature-toggles")]
    public async Task<ActionResult<FeatureToggleContract>> ChangeToggle([FromBody] ChangeFeatureToggleRequest request, CancellationToken cancellationToken)
        => Created(string.Empty, await mediator.Send(new ChangeFeatureToggleCommand(request.FeatureCode, request.IsEnabled), cancellationToken));

    [HttpPost("audit-logs")]
    public async Task<ActionResult<AuditLogEntryContract>> WriteAudit([FromBody] WriteAuditLogRequest request, CancellationToken cancellationToken)
        => Created(string.Empty, await mediator.Send(new WriteAuditLogEntryCommand(request.ActorUserId, request.ActionCode, request.Payload), cancellationToken));

    [HttpPost("school-year-lifecycle-policies")]
    public async Task<ActionResult<SchoolYearLifecyclePolicyContract>> ManageSchoolYearPolicy([FromBody] ManageSchoolYearLifecyclePolicyRequest request, CancellationToken cancellationToken)
        => Created(string.Empty, await mediator.Send(new ManageSchoolYearLifecyclePolicyCommand(request.SchoolId, request.PolicyName, request.ClosureGraceDays, request.Activate), cancellationToken));

    [HttpPost("housekeeping-policies")]
    public async Task<ActionResult<HousekeepingPolicyContract>> ManageHousekeepingPolicy([FromBody] ManageHousekeepingPolicyRequest request, CancellationToken cancellationToken)
        => Created(string.Empty, await mediator.Send(new ManageHousekeepingPolicyCommand(request.PolicyName, request.RetentionDays, request.Activate), cancellationToken));

    public sealed record ChangeSystemSettingRequest(string Key, string Value, bool IsSensitive);
    public sealed record ChangeFeatureToggleRequest(string FeatureCode, bool IsEnabled);
    public sealed record WriteAuditLogRequest(Guid ActorUserId, string ActionCode, string Payload);
    public sealed record ManageSchoolYearLifecyclePolicyRequest(Guid SchoolId, string PolicyName, int ClosureGraceDays, bool Activate);
    public sealed record ManageHousekeepingPolicyRequest(string PolicyName, int RetentionDays, bool Activate);
}
