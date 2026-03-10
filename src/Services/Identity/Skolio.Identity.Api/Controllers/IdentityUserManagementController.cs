using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Domain.Entities;
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
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserListItemContract>>> Users(
        [FromQuery] string? name,
        [FromQuery] string? emailOrUsername,
        [FromQuery] string? role,
        [FromQuery] string? accountStatus,
        [FromQuery] string? activationStatus,
        [FromQuery] string? blockStatus,
        [FromQuery] string? mfaStatus,
        [FromQuery] string? school,
        [FromQuery] string? schoolType,
        [FromQuery] string? inactivityState,
        [FromQuery] string? sortField = "name",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : 20;
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IQueryable<SkolioIdentityUser> queryable = userManager.Users.AsNoTracking();

        if (!User.IsInRole("PlatformAdministrator"))
        {
            var scopedUserIds = await ResolveActorScopedUserIds(cancellationToken);
            if (scopedUserIds.Count == 0)
            {
                return Ok(new PagedResult<UserListItemContract>(Array.Empty<UserListItemContract>(), normalizedPageNumber, normalizedPageSize, 0));
            }

            queryable = queryable.Where(x => scopedUserIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            queryable = queryable.Where(x =>
                (x.UserName ?? string.Empty).Contains(term)
                || dbContext.UserProfiles.Any(p => p.Id.ToString() == x.Id
                    && ((p.FirstName + " " + p.LastName).Contains(term)
                        || ((p.PreferredDisplayName ?? string.Empty).Contains(term)))));
        }

        if (!string.IsNullOrWhiteSpace(emailOrUsername))
        {
            var term = emailOrUsername.Trim();
            queryable = queryable.Where(x => (x.Email ?? string.Empty).Contains(term) || (x.UserName ?? string.Empty).Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            queryable = queryable.Where(x => dbContext.UserRoles.Any(ur => ur.UserId == x.Id && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == roleName)));
        }

        if (!string.IsNullOrWhiteSpace(accountStatus) && Enum.TryParse<IdentityAccountLifecycleStatus>(accountStatus, true, out var lifecycleStatus))
        {
            queryable = queryable.Where(x => x.AccountLifecycleStatus == lifecycleStatus);
        }

        if (!string.IsNullOrWhiteSpace(activationStatus))
        {
            var normalized = activationStatus.Trim().ToLowerInvariant();
            queryable = normalized switch
            {
                "active" => queryable.Where(x => x.ActivatedAtUtc != null),
                "pending" => queryable.Where(x => x.ActivatedAtUtc == null),
                _ => queryable
            };
        }

        if (!string.IsNullOrWhiteSpace(blockStatus))
        {
            var normalized = blockStatus.Trim().ToLowerInvariant();
            var now = DateTimeOffset.UtcNow;
            queryable = normalized switch
            {
                "blocked" => queryable.Where(x => x.BlockedAtUtc != null),
                "locked" => queryable.Where(x => x.LockoutEnd != null && x.LockoutEnd > now),
                "clear" => queryable.Where(x => x.BlockedAtUtc == null && (x.LockoutEnd == null || x.LockoutEnd <= now)),
                _ => queryable
            };
        }

        if (!string.IsNullOrWhiteSpace(mfaStatus))
        {
            var normalized = mfaStatus.Trim().ToLowerInvariant();
            queryable = normalized switch
            {
                "enabled" => queryable.Where(x => x.TwoFactorEnabled),
                "disabled" => queryable.Where(x => !x.TwoFactorEnabled),
                _ => queryable
            };
        }

        if (!string.IsNullOrWhiteSpace(school))
        {
            var schoolTerm = school.Trim();
            queryable = queryable.Where(x => dbContext.UserProfiles.Any(p => p.Id.ToString() == x.Id && (p.SchoolPlacement ?? string.Empty).Contains(schoolTerm))
                                           || dbContext.SchoolRoleAssignments.Any(a => a.UserProfileId.ToString() == x.Id && a.SchoolId.ToString() == schoolTerm));
        }

        if (!string.IsNullOrWhiteSpace(schoolType))
        {
            var schoolTypeTerm = schoolType.Trim();
            queryable = queryable.Where(x => dbContext.UserProfiles.Any(p => p.Id.ToString() == x.Id && (p.SchoolContextSummary ?? string.Empty).Contains(schoolTypeTerm)));
        }

        if (!string.IsNullOrWhiteSpace(inactivityState))
        {
            var normalized = inactivityState.Trim().ToLowerInvariant();
            var warningCutoff = DateTimeOffset.UtcNow.AddDays(-30);
            var inactiveCutoff = DateTimeOffset.UtcNow.AddDays(-90);
            queryable = normalized switch
            {
                "inactive" => queryable.Where(x => x.LastActivityAtUtc == null || x.LastActivityAtUtc < inactiveCutoff),
                "warning" => queryable.Where(x => x.LastActivityAtUtc != null && x.LastActivityAtUtc < warningCutoff && x.LastActivityAtUtc >= inactiveCutoff),
                "active" => queryable.Where(x => x.LastActivityAtUtc != null && x.LastActivityAtUtc >= warningCutoff),
                _ => queryable
            };
        }

        queryable = ApplySorting(queryable, sortField, descending);

        var totalCount = await queryable.CountAsync(cancellationToken);
        var pageItems = await queryable
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new
            {
                User = x,
                Profile = dbContext.UserProfiles.FirstOrDefault(p => p.Id.ToString() == x.Id)
            })
            .ToListAsync(cancellationToken);

        var userIds = pageItems.Select(x => x.User.Id).ToArray();
        var roleMap = await dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(x => x.RoleName).OrderBy(x => x).ToArray(), cancellationToken);

        var schoolMap = await dbContext.SchoolRoleAssignments
            .Where(x => userIds.Contains(x.UserProfileId.ToString()))
            .GroupBy(x => x.UserProfileId)
            .ToDictionaryAsync(g => g.Key.ToString(), g => (IReadOnlyCollection<string>)g.Select(x => x.SchoolId.ToString()).Distinct().OrderBy(x => x).ToArray(), cancellationToken);

        var items = pageItems.Select(x => new UserListItemContract(
            x.User.Id,
            x.User.Email ?? string.Empty,
            x.User.UserName ?? string.Empty,
            x.User.AccountLifecycleStatus.ToString(),
            x.User.EmailConfirmed,
            x.User.LockoutEnd,
            x.User.LastLoginAtUtc,
            x.User.LastActivityAtUtc,
            x.User.TwoFactorEnabled,
            x.User.ActivatedAtUtc,
            x.User.BlockedAtUtc,
            x.Profile is null ? string.Empty : $"{x.Profile.FirstName} {x.Profile.LastName}".Trim(),
            x.Profile?.SchoolPlacement,
            x.Profile?.SchoolContextSummary,
            roleMap.TryGetValue(x.User.Id, out var roles) ? roles : Array.Empty<string>(),
            schoolMap.TryGetValue(x.User.Id, out var schools) ? schools : Array.Empty<string>()))
            .ToArray();

        return Ok(new PagedResult<UserListItemContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    private IQueryable<SkolioIdentityUser> ApplySorting(IQueryable<SkolioIdentityUser> query, string? sortField, bool descending)
    {
        var normalized = sortField?.Trim().ToLowerInvariant() ?? "name";
        return normalized switch
        {
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "createdat" => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
            "lastlogin" => descending ? query.OrderByDescending(x => x.LastLoginAtUtc) : query.OrderBy(x => x.LastLoginAtUtc),
            "accountstatus" => descending ? query.OrderByDescending(x => x.AccountLifecycleStatus) : query.OrderBy(x => x.AccountLifecycleStatus),
            "school" => descending
                ? query.OrderByDescending(x => dbContext.UserProfiles.Where(p => p.Id.ToString() == x.Id).Select(p => p.SchoolPlacement).FirstOrDefault())
                : query.OrderBy(x => dbContext.UserProfiles.Where(p => p.Id.ToString() == x.Id).Select(p => p.SchoolPlacement).FirstOrDefault()),
            _ => descending
                ? query.OrderByDescending(x => dbContext.UserProfiles.Where(p => p.Id.ToString() == x.Id).Select(p => (p.PreferredDisplayName ?? (p.FirstName + " " + p.LastName))).FirstOrDefault())
                : query.OrderBy(x => dbContext.UserProfiles.Where(p => p.Id.ToString() == x.Id).Select(p => (p.PreferredDisplayName ?? (p.FirstName + " " + p.LastName))).FirstOrDefault())
        };
    }

    private async Task<IReadOnlyCollection<string>> ResolveActorScopedUserIds(CancellationToken cancellationToken)
    {
        var actorId = ActorId();
        var actorProfileId = TryParseGuid(actorId);
        if (actorProfileId is null) return Array.Empty<string>();

        var actorSchoolIds = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorProfileId.Value)
            .Select(x => x.SchoolId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (actorSchoolIds.Count == 0) return Array.Empty<string>();

        return await dbContext.SchoolRoleAssignments
            .Where(x => actorSchoolIds.Contains(x.SchoolId))
            .Select(x => x.UserProfileId.ToString())
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDetailContract>> UserDetail([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var roles = await userManager.GetRolesAsync(user);
        var profile = await dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id.ToString() == userId, cancellationToken);
        var schoolIds = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId.ToString() == userId)
            .Select(x => x.SchoolId.ToString())
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

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
            profile?.FirstName ?? string.Empty,
            profile?.LastName ?? string.Empty,
            profile?.SchoolPlacement,
            profile?.SchoolContextSummary,
            roles.OrderBy(x => x).ToArray(),
            schoolIds));
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
        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Active && user.BlockedAtUtc is null) return this.ValidationForm("Account is already active.");

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
        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Deactivated) return this.ValidationForm("Account is already deactivated.");

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
        if (user.BlockedAtUtc is not null) return this.ValidationForm("Account is already blocked.");

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
        if (user.BlockedAtUtc is null && (user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow)) return this.ValidationForm("Account is not blocked.");

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
        if (requested.Length == 0) return this.ValidationForm("At least one role is required.");
        if (!User.IsInRole("PlatformAdministrator") && requested.Contains("PlatformAdministrator", StringComparer.Ordinal))
        {
            return Forbid();
        }

        var requestedValidationError = await ValidateRoleSet(userId, requested, cancellationToken);
        if (!string.IsNullOrWhiteSpace(requestedValidationError)) return this.ValidationForm(requestedValidationError);

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

    [HttpPost("users/{userId}/roles/assign")]
    public async Task<IActionResult> AssignRole([FromRoute] string userId, [FromBody] RoleMutationRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var role = request.Role?.Trim() ?? string.Empty;
        if (!SupportedRoles.Contains(role, StringComparer.Ordinal)) return this.ValidationField("role", "Unsupported role.");
        if (!User.IsInRole("PlatformAdministrator") && string.Equals(role, "PlatformAdministrator", StringComparison.Ordinal)) return Forbid();
        if (!await roleManager.RoleExistsAsync(role)) return this.ValidationForm($"Role '{role}' does not exist.");

        var currentRoles = await userManager.GetRolesAsync(user);
        var nextRoles = currentRoles.Concat([role]).Distinct(StringComparer.Ordinal).ToArray();
        var validationError = await ValidateRoleSet(userId, nextRoles, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationError)) return this.ValidationForm(validationError);

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));

        Audit("identity.user-management.roles.assigned", user.Id, new { action = "assign-role", role });
        return Ok();
    }

    [HttpPost("users/{userId}/roles/remove")]
    public async Task<IActionResult> RemoveRole([FromRoute] string userId, [FromBody] RoleMutationRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, cancellationToken)) return Forbid();

        var role = request.Role?.Trim() ?? string.Empty;
        if (!SupportedRoles.Contains(role, StringComparer.Ordinal)) return this.ValidationField("role", "Unsupported role.");
        if (!User.IsInRole("PlatformAdministrator") && string.Equals(role, "PlatformAdministrator", StringComparison.Ordinal)) return Forbid();

        var currentRoles = await userManager.GetRolesAsync(user);
        var nextRoles = currentRoles.Where(x => !string.Equals(x, role, StringComparison.Ordinal)).ToArray();
        if (nextRoles.Length == 0) return this.ValidationForm("At least one role is required.");

        var validationError = await ValidateRoleSet(userId, nextRoles, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationError)) return this.ValidationForm(validationError);

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));

        Audit("identity.user-management.roles.removed", user.Id, new { action = "remove-role", role });
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
    private async Task<string?> ValidateRoleSet(string userId, IReadOnlyCollection<string> requestedRoles, CancellationToken cancellationToken)
    {
        var profileId = TryParseGuid(userId);
        if (profileId is null) return "User profile context is invalid.";

        var profile = await dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId.Value, cancellationToken);
        if (profile is null) return "User profile does not exist.";

        if (requestedRoles.Contains("Parent", StringComparer.Ordinal))
        {
            var hasParentLinks = await dbContext.ParentStudentLinks.AnyAsync(x => x.ParentUserProfileId == profile.Id, cancellationToken);
            if (!hasParentLinks) return "Parent role requires at least one ParentStudentLink.";
        }

        if (requestedRoles.Contains("Student", StringComparer.Ordinal) && profile.UserType != UserType.Student)
        {
            return "Student role requires a student profile context.";
        }

        if (requestedRoles.Contains("Teacher", StringComparer.Ordinal))
        {
            var hasTeacherAssignment = await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profile.Id && x.RoleCode == "Teacher", cancellationToken);
            if (!hasTeacherAssignment) return "Teacher role requires school teaching context assignment.";
        }

        if (requestedRoles.Contains("SchoolAdministrator", StringComparer.Ordinal))
        {
            var hasSchoolAdminScope = await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profile.Id && x.RoleCode == "SchoolAdministrator", cancellationToken);
            if (!hasSchoolAdminScope) return "SchoolAdministrator role requires school scope assignment.";
        }

        if (requestedRoles.Contains("PlatformAdministrator", StringComparer.Ordinal))
        {
            if (!User.IsInRole("PlatformAdministrator")) return "PlatformAdministrator role can be managed only by PlatformAdministrator.";
            if (profile.UserType != UserType.SupportStaff && profile.UserType != UserType.SchoolAdministrator)
            {
                return "PlatformAdministrator role requires governance profile context.";
            }
        }

        return null;
    }

    private static Guid? TryParseGuid(string value) => Guid.TryParse(value, out var parsed) ? parsed : null;
    private static string Display(SkolioIdentityUser user) => string.IsNullOrWhiteSpace(user.UserName) ? user.Email ?? user.Id : user.UserName;
    private void Audit(string actionCode, string targetId, object payload) => logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, ActorId(), targetId, payload);

    public sealed record UserListItemContract(string UserId, string Email, string UserName, string AccountLifecycleStatus, bool EmailConfirmed, DateTimeOffset? LockoutEndUtc, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, bool MfaEnabled, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? BlockedAtUtc, string DisplayName, string? School, string? SchoolType, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> SchoolIds);
    public sealed record UserDetailContract(string UserId, string Email, string UserName, bool EmailConfirmed, string AccountLifecycleStatus, DateTimeOffset? LockoutEndUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, string FirstName, string LastName, string? School, string? SchoolType, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> SchoolIds);
    public sealed record DeactivateRequest(string Reason);
    public sealed record BlockRequest(string? Reason);
    public sealed record UpdateRoleSetRequest(IReadOnlyCollection<string> Roles);
    public sealed record RoleMutationRequest(string Role);
    public sealed record LifecycleSummaryContract(string Status, DateTimeOffset? ActivationRequestedAtUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
