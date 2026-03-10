using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/user-management")]
[Authorize(Roles = "PlatformAdministrator,SchoolAdministrator")]
public sealed class IdentityUserManagementController(
    UserManager<SkolioIdentityUser> userManager,
    RoleManager<SkolioIdentityRole> roleManager,
    IdentityDbContext dbContext,
    IIdentityEmailSender identityEmailSender,
    ILogger<IdentityUserManagementController> logger) : ControllerBase
{
    private static readonly string[] SupportedRoles = ["PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"];

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyCollection<UserListItemContract>>> Users([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .Where(x => string.IsNullOrWhiteSpace(query) || (x.Email ?? string.Empty).Contains(query) || (x.UserName ?? string.Empty).Contains(query))
            .OrderBy(x => x.Email)
            .Take(300)
            .Select(x => new UserListItemContract(x.Id, x.Email ?? string.Empty, x.AccountLifecycleStatus.ToString(), x.EmailConfirmed, x.LockoutEnd, x.LastLoginAtUtc, x.LastActivityAtUtc))
            .ToListAsync(cancellationToken);

        var scoped = await FilterUsersByActorScope(users.Select(x => x.UserId).ToArray(), cancellationToken);
        return Ok(users.Where(x => scoped.Contains(x.UserId)).ToArray());
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDetailContract>> UserDetail([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserDetailContract(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.EmailConfirmed,
            user.AccountLifecycleStatus.ToString(),
            user.LockoutEnd,
            user.ActivatedAtUtc,
            user.DeactivatedAtUtc,
            user.DeactivationReason,
            user.BlockedAtUtc,
            user.BlockedReason,
            user.LastLoginAtUtc,
            user.LastActivityAtUtc,
            roles.OrderBy(x => x).ToArray()));
    }

    [HttpPost("users/{userId}/resend-activation")]
    public async Task<IActionResult> ResendActivation([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
        var link = $"http://localhost:8080/security/confirm-activation?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encoded)}";
        await identityEmailSender.SendAccountConfirmationAsync(new AccountConfirmationDelivery(user.Email ?? string.Empty, Display(user), link), cancellationToken);

        user.ActivationRequestedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        Audit("identity.user-management.activation.resent", user.Id, new { action = "resend-activation" });
        return Ok(new { message = "Activation email re-sent." });
    }

    [HttpPost("users/{userId}/activate")]
    public async Task<IActionResult> Activate([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
        user.ActivatedAtUtc = DateTimeOffset.UtcNow;
        user.DeactivatedAtUtc = null;
        user.DeactivationReason = null;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, Display(user), "Account activated", "Your Skolio account is active."), cancellationToken);
        Audit("identity.user-management.activated", user.Id, new { action = "activate" });
        return Ok();
    }

    [HttpPost("users/{userId}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] string userId, [FromBody] DeactivateRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason)) return this.ValidationField("reason", "Reason is required.");

        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Deactivated;
        user.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        user.DeactivationReason = request.Reason.Trim();
        user.DeactivatedByUserId = ActorId();
        await userManager.UpdateAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, Display(user), "Account deactivated", "Your Skolio account has been deactivated by administrator."), cancellationToken);
        Audit("identity.user-management.deactivated", user.Id, new { action = "deactivate", request.Reason });
        return Ok();
    }

    [HttpPost("users/{userId}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
        user.DeactivatedAtUtc = null;
        user.DeactivationReason = null;
        user.BlockedAtUtc = null;
        user.BlockedReason = null;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, Display(user), "Account reactivated", "Your Skolio account has been reactivated by administrator."), cancellationToken);
        Audit("identity.user-management.reactivated", user.Id, new { action = "reactivate" });
        return Ok();
    }

    [HttpPost("users/{userId}/block")]
    public async Task<IActionResult> Block([FromRoute] string userId, [FromBody] BlockRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Locked;
        user.BlockedAtUtc = DateTimeOffset.UtcNow;
        user.BlockedReason = request.Reason?.Trim();
        user.BlockedByUserId = ActorId();
        await userManager.UpdateAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, Display(user), "Account blocked", "Your Skolio account has been blocked by administrator."), cancellationToken);
        Audit("identity.user-management.blocked", user.Id, new { action = "block", request.Reason });
        return Ok();
    }

    [HttpPost("users/{userId}/unblock")]
    public async Task<IActionResult> Unblock([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        user.LockoutEnd = null;
        user.BlockedAtUtc = null;
        user.BlockedReason = null;
        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Locked)
        {
            user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
        }

        await userManager.UpdateAsync(user);
        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, Display(user), "Account unblocked", "Your Skolio account has been unblocked by administrator."), cancellationToken);
        Audit("identity.user-management.unblocked", user.Id, new { action = "unblock" });
        return Ok();
    }

    [HttpGet("users/{userId}/roles")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetRoles([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();
        return Ok((await userManager.GetRolesAsync(user)).OrderBy(x => x).ToArray());
    }

    [HttpPut("users/{userId}/roles")]
    public async Task<IActionResult> UpdateRoleSet([FromRoute] string userId, [FromBody] UpdateRoleSetRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var requested = request.Roles.Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Any(x => !SupportedRoles.Contains(x, StringComparer.Ordinal))) return this.ValidationForm("Unsupported role.");

        foreach (var role in requested)
        {
            if (!await roleManager.RoleExistsAsync(role)) return this.ValidationForm($"Role '{role}' does not exist.");
        }

        var current = await userManager.GetRolesAsync(user);
        var toRemove = current.Except(requested, StringComparer.Ordinal).ToArray();
        var toAdd = requested.Except(current, StringComparer.Ordinal).ToArray();

        if (toRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded) return BadRequest(removeResult.Errors.Select(x => x.Description));
        }

        if (toAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded) return BadRequest(addResult.Errors.Select(x => x.Description));
        }

        Audit("identity.user-management.roles.updated", user.Id, new { action = "update-role-set", toAdd, toRemove });
        return Ok();
    }

    [HttpGet("users/{userId}/lifecycle-summary")]
    public async Task<ActionResult<LifecycleSummaryContract>> LifecycleSummary([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        return Ok(new LifecycleSummaryContract(user.AccountLifecycleStatus.ToString(), user.ActivationRequestedAtUtc, user.ActivatedAtUtc, user.DeactivatedAtUtc, user.DeactivationReason, user.BlockedAtUtc, user.BlockedReason, user.LastLoginAtUtc, user.LastActivityAtUtc));
    }

    private async Task<HashSet<string>> FilterUsersByActorScope(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        if (User.IsInRole("PlatformAdministrator")) return userIds.ToHashSet(StringComparer.Ordinal);
        var actorId = ActorId();
        var actorProfileId = TryParseGuid(actorId);
        if (actorProfileId is null) return [];

        var schoolIds = await dbContext.SchoolRoleAssignments.Where(x => x.UserProfileId == actorProfileId.Value).Select(x => x.SchoolId).Distinct().ToListAsync(cancellationToken);
        if (schoolIds.Count == 0) return [];

        var candidateProfileIds = userIds.Select(TryParseGuid).OfType<Guid>().ToArray();
        var allowed = await dbContext.SchoolRoleAssignments.Where(x => candidateProfileIds.Contains(x.UserProfileId) && schoolIds.Contains(x.SchoolId)).Select(x => x.UserProfileId).Distinct().ToListAsync(cancellationToken);
        return allowed.Select(x => x.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> CanManageUser(string userId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("PlatformAdministrator")) return true;
        if (!User.IsInRole("SchoolAdministrator")) return false;
        if (string.Equals(ActorId(), userId, StringComparison.Ordinal)) return false;
        var scoped = await FilterUsersByActorScope([userId], cancellationToken);
        return scoped.Contains(userId);
    }

    private string ActorId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "anonymous";
    private static Guid? TryParseGuid(string value) => Guid.TryParse(value, out var parsed) ? parsed : null;
    private static string Display(SkolioIdentityUser user) => string.IsNullOrWhiteSpace(user.UserName) ? user.Email ?? user.Id : user.UserName;
    private void Audit(string actionCode, string targetId, object payload) => logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, ActorId(), targetId, payload);

    public sealed record UserListItemContract(string UserId, string Email, string AccountLifecycleStatus, bool EmailConfirmed, DateTimeOffset? LockoutEndUtc, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc);
    public sealed record UserDetailContract(string UserId, string Email, string UserName, bool EmailConfirmed, string AccountLifecycleStatus, DateTimeOffset? LockoutEndUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, IReadOnlyCollection<string> Roles);
    public sealed record DeactivateRequest(string Reason);
    public sealed record BlockRequest(string? Reason);
    public sealed record UpdateRoleSetRequest(IReadOnlyCollection<string> Roles);
    public sealed record LifecycleSummaryContract(string Status, DateTimeOffset? ActivationRequestedAtUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc);
}
