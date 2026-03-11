using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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

    [HttpGet("summary")]
    public async Task<ActionResult<UserManagementSummaryContract>> Summary([FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var scopeValidation = await ValidateRequestedSchoolContext(schoolContextId, cancellationToken);
        if (scopeValidation is not null) return scopeValidation;

        var scopedUsersQueryable = await BuildScopedUsersQueryable(schoolContextId, cancellationToken);
        var users = await scopedUsersQueryable.ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return Ok(new UserManagementSummaryContract(
            users.Count,
            users.Count(x => x.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Active && x.BlockedAtUtc == null && (x.LockoutEnd == null || x.LockoutEnd <= now)),
            users.Count(x => x.BlockedAtUtc != null || (x.LockoutEnd != null && x.LockoutEnd > now)),
            users.Count(x => x.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Deactivated),
            users.Count(x => x.ActivatedAtUtc == null),
            users.Count(x => x.TwoFactorEnabled)));
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserListItemContract>>> Users(
        [FromQuery] Guid? schoolContextId,
        [FromQuery] string? search,
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

        var scopeValidation = await ValidateRequestedSchoolContext(schoolContextId, cancellationToken);
        if (scopeValidation is not null) return scopeValidation;

        IQueryable<SkolioIdentityUser> queryable = await BuildScopedUsersQueryable(schoolContextId, cancellationToken);
        if (!User.IsInRole("PlatformAdministrator") && !await queryable.AnyAsync(cancellationToken))
        {
            return Ok(new PagedResult<UserListItemContract>(Array.Empty<UserListItemContract>(), normalizedPageNumber, normalizedPageSize, 0));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            var pattern = BuildSearchPattern(normalizedSearch);
            queryable = queryable.Where(x =>
                EF.Functions.ILike(x.UserName ?? string.Empty, pattern)
                || EF.Functions.ILike(x.Email ?? string.Empty, pattern)
                || dbContext.UserProfiles.Any(p => p.Id.ToString() == x.Id
                    && (EF.Functions.ILike(p.FirstName, pattern)
                        || EF.Functions.ILike(p.LastName, pattern)
                        || EF.Functions.ILike(p.PreferredDisplayName ?? string.Empty, pattern)
                        || EF.Functions.ILike((p.FirstName + " " + p.LastName), pattern)
                        || EF.Functions.ILike(p.SchoolContextSummary ?? string.Empty, pattern))));
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

    [HttpGet("schools")]
    public async Task<ActionResult<IReadOnlyCollection<SchoolContextOptionContract>>> Schools(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("PlatformAdministrator")) return Forbid();

        var items = await dbContext.SchoolRoleAssignments
            .AsNoTracking()
            .Select(x => x.SchoolId)
            .Distinct()
            .OrderBy(x => x)
            .Select(x => new SchoolContextOptionContract(x, x.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(items);
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

    private async Task<IQueryable<SkolioIdentityUser>> BuildScopedUsersQueryable(Guid? schoolContextId, CancellationToken cancellationToken)
    {
        IQueryable<SkolioIdentityUser> queryable = userManager.Users.AsNoTracking();
        if (User.IsInRole("PlatformAdministrator"))
        {
            if (schoolContextId is null) return queryable;

            var platformSchoolScopedUserIds = await ResolveScopedUserIdsForSchool(schoolContextId.Value, cancellationToken);
            return platformSchoolScopedUserIds.Count == 0 ? queryable.Where(_ => false) : queryable.Where(x => platformSchoolScopedUserIds.Contains(x.Id));
        }

        var actorScopedUserIds = await ResolveActorScopedUserIds(cancellationToken);
        if (actorScopedUserIds.Count == 0)
        {
            return queryable.Where(_ => false);
        }

        if (schoolContextId is null)
        {
            return queryable.Where(x => actorScopedUserIds.Contains(x.Id));
        }

        var schoolScopedUserIds = await ResolveScopedUserIdsForSchool(schoolContextId.Value, cancellationToken);
        return queryable.Where(x => actorScopedUserIds.Contains(x.Id) && schoolScopedUserIds.Contains(x.Id));
    }

    private async Task<HashSet<string>> ResolveScopedUserIdsForSchool(Guid schoolContextId, CancellationToken cancellationToken)
        => await dbContext.SchoolRoleAssignments
            .Where(x => x.SchoolId == schoolContextId)
            .Select(x => x.UserProfileId.ToString())
            .ToHashSetAsync(cancellationToken);

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
    public async Task<ActionResult<UserDetailContract>> UserDetail([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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
            profile?.PreferredDisplayName,
            profile?.PreferredLanguage,
            profile?.PhoneNumber,
            profile?.ContactEmail,
            profile?.SchoolPlacement,
            profile?.SchoolContextSummary,
            roles.OrderBy(x => x).ToArray(),
            schoolIds));
    }

    [HttpPut("users/{userId}/basic-profile")]
    public async Task<ActionResult<UserDetailContract>> UpdateBasicProfile([FromRoute] string userId, [FromBody] UpdateBasicProfileRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var profileId = TryParseGuid(userId);
        if (profileId is null) return this.ValidationForm("User profile context is invalid.");

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == profileId.Value, cancellationToken);
        if (profile is null) return this.ValidationForm("User profile does not exist.");

        if (string.IsNullOrWhiteSpace(request.FirstName)) return this.ValidationField("firstName", "First name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName)) return this.ValidationField("lastName", "Last name is required.");

        profile.Update(
            request.FirstName,
            request.LastName,
            profile.UserType,
            profile.Email,
            request.PreferredDisplayName,
            request.PreferredLanguage,
            request.PhoneNumber,
            profile.Gender,
            profile.DateOfBirth,
            profile.NationalIdNumber,
            profile.BirthPlace,
            profile.PermanentAddress,
            profile.CorrespondenceAddress,
            request.ContactEmail,
            profile.LegalGuardian1,
            profile.LegalGuardian2,
            request.SchoolPlacement,
            profile.HealthInsuranceProvider,
            profile.Pediatrician,
            profile.HealthSafetyNotes,
            profile.SupportMeasuresSummary,
            request.PositionTitle,
            profile.TeacherRoleLabel,
            profile.QualificationSummary,
            request.SchoolContextSummary,
            request.ParentRelationshipSummary,
            profile.DeliveryContactName,
            profile.DeliveryContactPhone,
            profile.PreferredContactChannel,
            profile.CommunicationPreferencesSummary,
            profile.PublicContactNote,
            profile.PreferredContactNote,
            profile.AdministrativeWorkDesignation,
            profile.AdministrativeOrganizationSummary,
            profile.PlatformRoleContextSummary,
            profile.ManagedPlatformAreasSummary,
            profile.AdministrativeBoundarySummary);

        await dbContext.SaveChangesAsync(cancellationToken);
        Audit("identity.user-management.basic-profile.updated", user.Id, new { action = "update-basic-profile" });

        return await UserDetail(userId, schoolContextId, cancellationToken);
    }


    [HttpGet("users/{userId}/roles-detail")]
    public async Task<ActionResult<UserRolesDetailContract>> UserRolesDetail([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserRolesDetailContract(
            roles.OrderBy(x => x).ToArray(),
            SupportedRoles,
            User.IsInRole("PlatformAdministrator")));
    }

    [HttpGet("users/{userId}/lifecycle-detail")]
    public async Task<ActionResult<LifecycleSummaryContract>> UserLifecycleDetail([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        return Ok(new LifecycleSummaryContract(
            user.AccountLifecycleStatus.ToString(),
            user.ActivationRequestedAtUtc,
            user.ActivatedAtUtc,
            user.DeactivatedAtUtc,
            user.DeactivationReason,
            user.BlockedAtUtc,
            user.BlockedReason,
            user.LastLoginAtUtc,
            user.LastActivityAtUtc));
    }

    [HttpGet("users/{userId}/security-detail")]
    public async Task<ActionResult<UserSecurityDetailContract>> UserSecurityDetail([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        return Ok(new UserSecurityDetailContract(
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd,
            user.LastLoginAtUtc,
            user.LastActivityAtUtc,
            "Recovery codes summary is not exposed in admin API."));
    }

    [HttpGet("users/{userId}/school-context-detail")]
    public async Task<ActionResult<UserSchoolContextDetailContract>> UserSchoolContextDetail([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id.ToString() == userId, cancellationToken);
        var schoolIds = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId.ToString() == userId)
            .Select(x => x.SchoolId.ToString())
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

        return Ok(new UserSchoolContextDetailContract(
            profile?.SchoolPlacement,
            profile?.SchoolContextSummary,
            schoolIds,
            User.IsInRole("PlatformAdministrator")));
    }

    [HttpGet("users/{userId}/links-summary")]
    public async Task<ActionResult<UserLinksSummaryContract>> UserLinksSummary([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var profileId = TryParseGuid(userId);
        if (profileId is null) return this.ValidationForm("User profile context is invalid.");

        var parentLinks = await dbContext.ParentStudentLinks.CountAsync(x => x.ParentUserProfileId == profileId.Value, cancellationToken);
        var teacherAssignments = await dbContext.SchoolRoleAssignments.CountAsync(x => x.UserProfileId == profileId.Value && x.RoleCode == "Teacher", cancellationToken);
        var studentAssignments = await dbContext.SchoolRoleAssignments.CountAsync(x => x.UserProfileId == profileId.Value && x.RoleCode == "Student", cancellationToken);

        return Ok(new UserLinksSummaryContract(parentLinks, teacherAssignments, studentAssignments));
    }

    [HttpPut("users/{userId}/school-context")]
    public async Task<IActionResult> UpdateSchoolContext([FromRoute] string userId, [FromBody] UpdateSchoolContextRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var profileId = TryParseGuid(userId);
        if (profileId is null) return this.ValidationForm("User profile context is invalid.");

        var requestedSchoolIds = request.SchoolIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (requestedSchoolIds.Length == 0) return this.ValidationField("schoolIds", "At least one school assignment is required.");

        if (User.IsInRole("SchoolAdministrator"))
        {
            var actorSchoolIds = await ResolveActorSchoolIds(cancellationToken);
            if (requestedSchoolIds.Any(x => !actorSchoolIds.Contains(x))) return Forbid();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var schoolScopedRoles = currentRoles.Where(x => x is "SchoolAdministrator" or "Teacher" or "Parent" or "Student").Distinct(StringComparer.Ordinal).ToArray();
        if (schoolScopedRoles.Length == 0) return this.ValidationForm("Current role set does not allow school scoped assignment.");

        var existing = await dbContext.SchoolRoleAssignments.Where(x => x.UserProfileId == profileId.Value).ToListAsync(cancellationToken);
        dbContext.SchoolRoleAssignments.RemoveRange(existing);
        foreach (var nextSchoolId in requestedSchoolIds)
        {
            foreach (var roleCode in schoolScopedRoles)
            {
                dbContext.SchoolRoleAssignments.Add(SchoolRoleAssignment.Create(Guid.NewGuid(), profileId.Value, nextSchoolId, roleCode));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        Audit("identity.user-management.school-context.updated", user.Id, new { action = "update-school-context", requestedSchoolIds });
        return Ok();
    }

    [HttpPut("users/{userId}/links/parent-students")]
    public async Task<IActionResult> UpdateParentStudentLinks([FromRoute] string userId, [FromBody] UpdateParentLinksRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var profileId = TryParseGuid(userId);
        if (profileId is null) return this.ValidationForm("User profile context is invalid.");

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains("Parent", StringComparer.Ordinal)) return this.ValidationForm("Parent links can be edited only for users with Parent role.");

        var requestedLinks = request.Links
            .Where(x => x.StudentUserProfileId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Relationship))
            .GroupBy(x => x.StudentUserProfileId)
            .Select(x => x.First())
            .ToArray();

        if (requestedLinks.Length == 0) return this.ValidationField("links", "At least one parent-student link is required.");

        var studentIds = requestedLinks.Select(x => x.StudentUserProfileId).Distinct().ToArray();
        var validStudentIds = await dbContext.UserProfiles
            .Where(x => studentIds.Contains(x.Id) && x.UserType == UserType.Student)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        if (validStudentIds.Length != studentIds.Length) return this.ValidationField("links", "All linked profiles must be student profiles.");

        var existing = await dbContext.ParentStudentLinks.Where(x => x.ParentUserProfileId == profileId.Value).ToListAsync(cancellationToken);
        dbContext.ParentStudentLinks.RemoveRange(existing);
        foreach (var link in requestedLinks)
        {
            dbContext.ParentStudentLinks.Add(ParentStudentLink.Create(Guid.NewGuid(), profileId.Value, link.StudentUserProfileId, link.Relationship));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        Audit("identity.user-management.links.parent-student.updated", user.Id, new { action = "update-parent-student-links", links = requestedLinks.Select(x => x.StudentUserProfileId) });
        return Ok();
    }

    [HttpPost("users/{userId}/security/unlock-lockout")]
    public async Task<IActionResult> UnlockSecurityLockout([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));

        Audit("identity.user-management.security.lockout-cleared", user.Id, new { action = "clear-lockout" });
        return Ok();
    }

    [HttpPost("users/{userId}/security/disable-mfa")]
    public async Task<IActionResult> DisableMfaForUser([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

        var disableResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded) return BadRequest(disableResult.Errors.Select(x => x.Description));

        await userManager.ResetAuthenticatorKeyAsync(user);
        Audit("identity.user-management.security.mfa-disabled", user.Id, new { action = "disable-mfa" });
        return Ok();
    }

    [HttpPost("users/{userId}/resend-activation")]
    public async Task<IActionResult> ResendActivation([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Active) return this.ValidationForm("Invite cannot be resent for an active account.");
        if (user.InviteSentAtUtc is not null && user.InviteSentAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1)) return this.ValidationForm("Invite resend is temporarily limited.");

        await DispatchInviteEmail(user, cancellationToken);
        Audit("identity.user-management.invite.resent", user.Id, new { action = "resend-activation" });
        return Ok(new { message = "Activation invite re-sent." });
    }

    [HttpPost("users/{userId}/activate")]
    public async Task<IActionResult> Activate([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
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
    public async Task<IActionResult> Deactivate([FromRoute] string userId, [FromBody] DeactivateRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
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
    public async Task<IActionResult> Reactivate([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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
    public async Task<IActionResult> Block([FromRoute] string userId, [FromBody] BlockRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
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
    public async Task<IActionResult> Unblock([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
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
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetRoles([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();
        return Ok((await userManager.GetRolesAsync(user)).OrderBy(x => x).ToArray());
    }

    [HttpPut("users/{userId}/roles")]
    public async Task<IActionResult> UpdateRoleSet([FromRoute] string userId, [FromBody] UpdateRoleSetRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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
    public async Task<IActionResult> AssignRole([FromRoute] string userId, [FromBody] RoleMutationRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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
    public async Task<IActionResult> RemoveRole([FromRoute] string userId, [FromBody] RoleMutationRequest request, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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
    public async Task<ActionResult<LifecycleSummaryContract>> LifecycleSummary([FromRoute] string userId, [FromQuery] Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (!await CanManageUser(userId, schoolContextId, cancellationToken)) return Forbid();

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

    private async Task<bool> CanManageUser(string userId, Guid? schoolContextId, CancellationToken cancellationToken)
    {
        var scopeValidation = await ValidateRequestedSchoolContext(schoolContextId, cancellationToken);
        if (scopeValidation is not null) return false;
        if (User.IsInRole("PlatformAdministrator"))
        {
            if (schoolContextId is null) return true;
            return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId.ToString() == userId && x.SchoolId == schoolContextId.Value, cancellationToken);
        }
        if (!User.IsInRole("SchoolAdministrator")) return false;
        if (string.Equals(ActorId(), userId, StringComparison.Ordinal)) return false;
        if (schoolContextId.HasValue)
        {
            var scopedIds = await ResolveActorSchoolIds(cancellationToken);
            if (!scopedIds.Contains(schoolContextId.Value)) return false;
            return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId.ToString() == userId && x.SchoolId == schoolContextId.Value, cancellationToken);
        }

        var scoped = await FilterUsersByActorScope([userId], cancellationToken);
        return scoped.Contains(userId);
    }

    private async Task<ActionResult?> ValidateRequestedSchoolContext(Guid? schoolContextId, CancellationToken cancellationToken)
    {
        if (schoolContextId is null) return null;

        if (User.IsInRole("PlatformAdministrator"))
        {
            var schoolExists = await dbContext.SchoolRoleAssignments.AnyAsync(x => x.SchoolId == schoolContextId.Value, cancellationToken);
            return schoolExists ? null : this.ValidationForm("Requested school context is not available.");
        }

        if (!User.IsInRole("SchoolAdministrator")) return Forbid();

        var actorSchoolIds = await ResolveActorSchoolIds(cancellationToken);
        return actorSchoolIds.Contains(schoolContextId.Value) ? null : Forbid();
    }

    private async Task<HashSet<Guid>> ResolveActorSchoolIds(CancellationToken cancellationToken)
    {
        var actorProfileId = TryParseGuid(ActorId());
        if (actorProfileId is null) return [];

        return await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorProfileId.Value)
            .Select(x => x.SchoolId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);
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


    private async Task DispatchInviteEmail(SkolioIdentityUser user, CancellationToken cancellationToken)
    {
        var inviteToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var inviteTokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(inviteToken));
        var activationCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddHours(24);

        user.ActivationRequestedAtUtc = now;
        user.InviteSentAtUtc = now;
        user.InviteExpiresAtUtc = expiresAt;
        user.InviteConfirmedAtUtc = null;
        user.InviteTokenHash = HashSecret(inviteTokenEncoded);
        user.InviteCodeHash = HashSecret(activationCode);
        user.InviteStatus = IdentityInviteStatus.InviteSent;
        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.PendingActivation;

        await userManager.UpdateAsync(user);

        var origin = Request.Headers.Origin.FirstOrDefault();
        var baseUrl = string.IsNullOrWhiteSpace(origin) ? "http://localhost:8080" : origin.TrimEnd('/');
        var inviteLink = $"{baseUrl}/security/invite-activation?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(inviteTokenEncoded)}";

        await identityEmailSender.SendAccountInviteAsync(new AccountInviteDelivery(
            user.Email ?? string.Empty,
            Display(user),
            inviteLink,
            activationCode,
            expiresAt.ToString("O")), cancellationToken);
    }

    private static string HashSecret(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
    private static Guid? TryParseGuid(string value) => Guid.TryParse(value, out var parsed) ? parsed : null;
    private static string BuildSearchPattern(string term)
        => $"%{term.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";

    private static string Display(SkolioIdentityUser user) => string.IsNullOrWhiteSpace(user.UserName) ? user.Email ?? user.Id : user.UserName;
    private void Audit(string actionCode, string targetId, object payload) => logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, ActorId(), targetId, payload);

    public sealed record UserListItemContract(string UserId, string Email, string UserName, string AccountLifecycleStatus, bool EmailConfirmed, DateTimeOffset? LockoutEndUtc, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, bool MfaEnabled, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? BlockedAtUtc, string DisplayName, string? School, string? SchoolType, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> SchoolIds);
    public sealed record UserDetailContract(string UserId, string Email, string UserName, bool EmailConfirmed, string AccountLifecycleStatus, DateTimeOffset? LockoutEndUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, string FirstName, string LastName, string? PreferredDisplayName, string? PreferredLanguage, string? PhoneNumber, string? ContactEmail, string? School, string? SchoolType, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> SchoolIds);
    public sealed record DeactivateRequest(string Reason);
    public sealed record BlockRequest(string? Reason);
    public sealed record UpdateRoleSetRequest(IReadOnlyCollection<string> Roles);
    public sealed record RoleMutationRequest(string Role);
    public sealed record UpdateBasicProfileRequest(string FirstName, string LastName, string? PreferredDisplayName, string? PreferredLanguage, string? PhoneNumber, string? ContactEmail, string? SchoolPlacement, string? SchoolContextSummary, string? PositionTitle, string? ParentRelationshipSummary);
    public sealed record UpdateSchoolContextRequest(IReadOnlyCollection<Guid> SchoolIds);
    public sealed record ParentLinkMutationRequest(Guid StudentUserProfileId, string Relationship);
    public sealed record UpdateParentLinksRequest(IReadOnlyCollection<ParentLinkMutationRequest> Links);
    public sealed record LifecycleSummaryContract(string Status, DateTimeOffset? ActivationRequestedAtUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? DeactivatedAtUtc, string? DeactivationReason, DateTimeOffset? BlockedAtUtc, string? BlockedReason, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc);
    public sealed record UserRolesDetailContract(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> AvailableRoles, bool CanManagePlatformAdministratorRole);
    public sealed record UserSecurityDetailContract(bool EmailConfirmed, bool MfaEnabled, DateTimeOffset? LockoutEndUtc, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastActivityAtUtc, string RecoveryCodesSummary);
    public sealed record UserSchoolContextDetailContract(string? School, string? SchoolType, IReadOnlyCollection<string> SchoolIds, bool IsPlatformScopeView);
    public sealed record UserLinksSummaryContract(int ParentStudentLinksCount, int TeacherAssignmentCount, int StudentAssignmentCount);
    public sealed record UserManagementSummaryContract(int TotalUsersCount, int ActiveUsersCount, int LockedUsersCount, int DeactivatedUsersCount, int PendingActivationUsersCount, int MfaEnabledUsersCount);
    public sealed record SchoolContextOptionContract(Guid SchoolId, string Label);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    // ── Create User Wizard contracts ──────────────────────────────────────────

    public sealed record CreateUserWizardRequest(
        // Step 1 – basic account
        string Email,
        string UserName,
        string FirstName,
        string LastName,
        string? DisplayName,
        string? PreferredLanguage,
        // Step 2 – role and scope
        string Role,
        Guid? SchoolId,
        // Step 3 – profile data
        string? PhoneNumber,
        string? PositionTitle,
        string? SchoolPlacement,
        string? SchoolContextSummary,
        string? ParentRelationshipSummary,
        string? ContactEmail,
        // Step 4 – role-specific links
        Guid? LinkedStudentProfileId,
        string? ParentStudentRelationship,
        // Step 5 – activation
        string ActivationPolicy);

    public sealed record CreateUserWizardResult(
        string UserId,
        string Email,
        string UserName,
        string DisplayName,
        string Role,
        string AccountLifecycleStatus,
        bool ActivationEmailSent);

    public sealed record WizardStudentCandidateContract(
        string ProfileId,
        string DisplayName,
        string Email,
        string? SchoolPlacement);

    // ── Create User Wizard endpoints ──────────────────────────────────────────

    [HttpGet("create-wizard/student-candidates")]
    public async Task<ActionResult<IReadOnlyCollection<WizardStudentCandidateContract>>> WizardStudentCandidates(
        [FromQuery] Guid? schoolId,
        CancellationToken cancellationToken)
    {
        if (schoolId.HasValue)
        {
            var scopeValidation = await ValidateRequestedSchoolContext(schoolId, cancellationToken);
            if (scopeValidation is not null) return scopeValidation;
        }

        IQueryable<UserProfile> profileQuery = dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.UserType == UserType.Student && x.IsActive);

        if (schoolId.HasValue)
        {
            var studentIdsInSchool = await dbContext.SchoolRoleAssignments
                .Where(x => x.SchoolId == schoolId.Value && x.RoleCode == "Student")
                .Select(x => x.UserProfileId)
                .ToListAsync(cancellationToken);

            profileQuery = profileQuery.Where(x => studentIdsInSchool.Contains(x.Id));
        }
        else if (!User.IsInRole("PlatformAdministrator"))
        {
            var actorProfileId = TryParseGuid(ActorId());
            if (actorProfileId is null) return Forbid();

            var actorSchoolIds = await dbContext.SchoolRoleAssignments
                .Where(x => x.UserProfileId == actorProfileId.Value)
                .Select(x => x.SchoolId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var studentIdsInScope = await dbContext.SchoolRoleAssignments
                .Where(x => actorSchoolIds.Contains(x.SchoolId) && x.RoleCode == "Student")
                .Select(x => x.UserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            profileQuery = profileQuery.Where(x => studentIdsInScope.Contains(x.Id));
        }

        var profiles = await profileQuery
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Take(200)
            .ToListAsync(cancellationToken);

        var result = profiles
            .Select(p => new WizardStudentCandidateContract(
                p.Id.ToString(),
                $"{p.FirstName} {p.LastName}".Trim(),
                p.Email,
                p.SchoolPlacement))
            .ToArray();

        return Ok(result);
    }

    [HttpPost("create-wizard")]
    public async Task<ActionResult<CreateUserWizardResult>> CreateWizard(
        [FromBody] CreateUserWizardRequest request,
        CancellationToken cancellationToken)
    {
        // ── Step 1 validation ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Email))
            return this.ValidationField("email", "Email is required.");
        if (string.IsNullOrWhiteSpace(request.UserName))
            return this.ValidationField("userName", "Username is required.");
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return this.ValidationField("firstName", "First name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName))
            return this.ValidationField("lastName", "Last name is required.");

        // ── Step 2 validation ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Role))
            return this.ValidationField("role", "Role is required.");
        if (!SupportedRoles.Contains(request.Role, StringComparer.Ordinal))
            return this.ValidationField("role", "Unsupported role.");
        if (string.Equals(request.Role, "PlatformAdministrator", StringComparison.Ordinal)
            && !User.IsInRole("PlatformAdministrator"))
            return Forbid();

        // Resolve and validate school scope
        Guid? resolvedSchoolId = request.SchoolId;
        if (!User.IsInRole("PlatformAdministrator"))
        {
            var actorSchoolIds = await ResolveActorSchoolIds(cancellationToken);
            if (actorSchoolIds.Count == 0) return Forbid();

            if (resolvedSchoolId.HasValue && !actorSchoolIds.Contains(resolvedSchoolId.Value))
                return this.ValidationField("schoolId", "You do not have permission to create users in the selected school.");

            if (!resolvedSchoolId.HasValue)
                resolvedSchoolId = actorSchoolIds.First();
        }

        if (!resolvedSchoolId.HasValue
            && !string.Equals(request.Role, "PlatformAdministrator", StringComparison.Ordinal))
            return this.ValidationField("schoolId", "School scope is required for this role.");

        // ── Activation policy validation ───────────────────────────────────
        var activationPolicy = string.IsNullOrWhiteSpace(request.ActivationPolicy)
            ? "SendActivationEmail"
            : request.ActivationPolicy.Trim();

        if (!string.Equals(activationPolicy, "SendActivationEmail", StringComparison.Ordinal))
            return this.ValidationField("activationPolicy", "Only SendActivationEmail policy is allowed for admin onboarding.");

        // ── Parent role: linked student required ───────────────────────────
        if (string.Equals(request.Role, "Parent", StringComparison.Ordinal))
        {
            if (!request.LinkedStudentProfileId.HasValue || request.LinkedStudentProfileId.Value == Guid.Empty)
                return this.ValidationField("linkedStudentProfileId", "Linked student is required for Parent role.");
            if (string.IsNullOrWhiteSpace(request.ParentStudentRelationship))
                return this.ValidationField("parentStudentRelationship", "Relationship type is required for Parent role.");

            var studentExists = await dbContext.UserProfiles
                .AnyAsync(x => x.Id == request.LinkedStudentProfileId.Value && x.UserType == UserType.Student, cancellationToken);
            if (!studentExists)
                return this.ValidationField("linkedStudentProfileId", "Selected student profile does not exist or is not a student.");
        }

        // ── Uniqueness checks ──────────────────────────────────────────────
        var existingByEmail = await userManager.FindByEmailAsync(request.Email.Trim());
        if (existingByEmail is not null)
            return this.ValidationField("email", "This email address is already in use.");

        var existingByName = await userManager.FindByNameAsync(request.UserName.Trim());
        if (existingByName is not null)
            return this.ValidationField("userName", "This username is already in use.");

        // ── Role existence check ───────────────────────────────────────────
        if (!await roleManager.RoleExistsAsync(request.Role))
            return this.ValidationField("role", $"Role '{request.Role}' is not configured in the system.");

        // ── Create SkolioIdentityUser ──────────────────────────────────────
        var newUser = new SkolioIdentityUser
        {
            Email = request.Email.Trim(),
            UserName = request.UserName.Trim(),
            AccountLifecycleStatus = IdentityAccountLifecycleStatus.PendingActivation
        };

        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(x => x.Description));

        Audit("identity.user-management.create-wizard.user-created", newUser.Id,
            new { action = "create-wizard.user-created", email = newUser.Email, role = request.Role });

        // ── Map UserType from role ─────────────────────────────────────────
        var userType = request.Role switch
        {
            "Teacher"              => UserType.Teacher,
            "Parent"               => UserType.Parent,
            "Student"              => UserType.Student,
            "SchoolAdministrator"  => UserType.SchoolAdministrator,
            "PlatformAdministrator" => UserType.SupportStaff,
            _                      => UserType.Teacher
        };

        // ── Create UserProfile ─────────────────────────────────────────────
        var profileId = Guid.Parse(newUser.Id);
        var profile = UserProfile.Create(
            profileId,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            userType,
            request.Email.Trim(),
            preferredDisplayName: request.DisplayName?.Trim(),
            preferredLanguage: request.PreferredLanguage?.Trim(),
            phoneNumber: request.PhoneNumber?.Trim(),
            gender: null,
            dateOfBirth: null,
            nationalIdNumber: null,
            birthPlace: null,
            permanentAddress: null,
            correspondenceAddress: null,
            contactEmail: request.ContactEmail?.Trim(),
            legalGuardian1: null,
            legalGuardian2: null,
            schoolPlacement: request.SchoolPlacement?.Trim(),
            healthInsuranceProvider: null,
            pediatrician: null,
            healthSafetyNotes: null,
            supportMeasuresSummary: null,
            positionTitle: request.PositionTitle?.Trim(),
            teacherRoleLabel: null,
            qualificationSummary: null,
            schoolContextSummary: request.SchoolContextSummary?.Trim(),
            parentRelationshipSummary: request.ParentRelationshipSummary?.Trim(),
            deliveryContactName: null,
            deliveryContactPhone: null,
            preferredContactChannel: null,
            communicationPreferencesSummary: null,
            publicContactNote: null,
            preferredContactNote: null,
            administrativeWorkDesignation: null,
            administrativeOrganizationSummary: null,
            platformRoleContextSummary: null,
            managedPlatformAreasSummary: null,
            administrativeBoundarySummary: null);

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.user-management.create-wizard.profile-created", newUser.Id,
            new { action = "create-wizard.profile-created", userType });

        // ── Create SchoolRoleAssignment ────────────────────────────────────
        if (resolvedSchoolId.HasValue
            && !string.Equals(request.Role, "PlatformAdministrator", StringComparison.Ordinal))
        {
            var assignment = SchoolRoleAssignment.Create(
                Guid.NewGuid(), profileId, resolvedSchoolId.Value, request.Role);
            dbContext.SchoolRoleAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);

            Audit("identity.user-management.create-wizard.school-assignment-created", newUser.Id,
                new { action = "create-wizard.school-assignment-created", schoolId = resolvedSchoolId, roleCode = request.Role });
        }

        // ── Create ParentStudentLink ───────────────────────────────────────
        if (string.Equals(request.Role, "Parent", StringComparison.Ordinal)
            && request.LinkedStudentProfileId.HasValue
            && request.LinkedStudentProfileId.Value != Guid.Empty)
        {
            var relationship = request.ParentStudentRelationship!.Trim();
            var link = ParentStudentLink.Create(
                Guid.NewGuid(), profileId, request.LinkedStudentProfileId.Value, relationship);
            dbContext.ParentStudentLinks.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);

            Audit("identity.user-management.create-wizard.parent-link-created", newUser.Id,
                new { action = "create-wizard.parent-link-created", studentId = request.LinkedStudentProfileId });
        }

        // ── Assign Identity role ───────────────────────────────────────────
        var roleAssignResult = await userManager.AddToRoleAsync(newUser, request.Role);
        if (!roleAssignResult.Succeeded)
            return BadRequest(roleAssignResult.Errors.Select(x => x.Description));

        Audit("identity.user-management.create-wizard.role-assigned", newUser.Id,
            new { action = "create-wizard.role-assigned", role = request.Role });

        // ── Activation ────────────────────────────────────────────────────
        await DispatchInviteEmail(newUser, cancellationToken);
        var activationEmailSent = true;
        Audit("identity.user-management.create-wizard.activation-email-sent", newUser.Id,
            new { action = "create-wizard.activation-email-sent" });

        // ── Final audit ───────────────────────────────────────────────────
        Audit("identity.user-management.create-wizard.completed", newUser.Id,
            new { action = "create-wizard.completed", activationPolicy, role = request.Role, schoolId = resolvedSchoolId });

        var displayName = !string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.DisplayName.Trim()
            : $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim();

        return StatusCode(201, new CreateUserWizardResult(
            newUser.Id,
            newUser.Email ?? string.Empty,
            newUser.UserName ?? string.Empty,
            displayName,
            request.Role,
            newUser.AccountLifecycleStatus.ToString(),
            activationEmailSent));
    }
}
