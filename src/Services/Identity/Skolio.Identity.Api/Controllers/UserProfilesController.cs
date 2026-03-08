using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator, IdentityDbContext dbContext, ILogger<UserProfilesController> logger) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> Me(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpGet("me/summary")]
    [Authorize]
    public async Task<ActionResult<MyProfileSummaryContract>> MySummary(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        var parentStudentLinks = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId || x.StudentUserProfileId == actorUserId)
            .OrderBy(x => x.ParentUserProfileId)
            .ThenBy(x => x.StudentUserProfileId)
            .Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship))
            .ToListAsync(cancellationToken);

        var schoolIds = roleAssignments.Select(x => x.SchoolId).Distinct().ToList();

        return Ok(new MyProfileSummaryContract(
            ToContract(profile),
            roleAssignments,
            parentStudentLinks,
            schoolIds,
            SchoolScope.IsPlatformAdministrator(User),
            User.IsInRole("SchoolAdministrator"),
            User.IsInRole("Teacher"),
            User.IsInRole("Parent"),
            User.IsInRole("Student")));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeSelfRequest(profile, request);
        var changedFields = CollectChangedFields(profile, normalizedRequest);

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.PositionTitle,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote), cancellationToken);

        Audit("identity.user-profile.self-updated", profile.Id, new { changedFields });
        return Ok(result);
    }

    [HttpGet("linked-students")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> LinkedStudents(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Parent")) return Forbid();

        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var linkedStudentIds = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId)
            .Select(x => x.StudentUserProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (linkedStudentIds.Count == 0) return Ok(Array.Empty<UserProfileContract>());

        var result = await dbContext.UserProfiles
            .Where(x => linkedStudentIds.Contains(x.Id))
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.PositionTitle,
                x.PublicContactNote,
                x.PreferredContactNote))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("student-context")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<StudentContextContract>> StudentContext(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        return Ok(new StudentContextContract(ToContract(profile), roleAssignments));
    }

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> List([FromQuery] bool? isActive, [FromQuery] UserType? userType, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.UserProfiles.AsQueryable();

        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            var scopedProfileIds = await dbContext.SchoolRoleAssignments
                .Where(x => scopedSchoolIds.Contains(x.SchoolId))
                .Select(x => x.UserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => scopedProfileIds.Contains(x.Id));
        }

        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (userType.HasValue) query = query.Where(x => x.UserType == userType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.FirstName, $"%{term}%") || EF.Functions.ILike(x.LastName, $"%{term}%") || EF.Functions.ILike(x.Email, $"%{term}%"));
        }

        var result = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.PositionTitle,
                x.PublicContactNote,
                x.PreferredContactNote))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Update(Guid id, [FromBody] UpdateAdminProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeAdminRequest(request);
        var changedFields = CollectChangedFields(profile, normalizedRequest);

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.PositionTitle,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote), cancellationToken);

        Audit("identity.user-profile.admin-updated", id, new { changedFields });
        return Ok(result);
    }

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> SetActivation(Guid id, [FromBody] SetActivationRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        if (request.IsActive) profile.Activate(); else profile.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "identity.user-profile.activated" : "identity.user-profile.deactivated", id, new { request.IsActive });
        return Ok(ToContract(profile));
    }

    private async Task<bool> HasProfileAccess(Guid profileId, CancellationToken cancellationToken)
    {
        if (SchoolScope.IsPlatformAdministrator(User)) return true;

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
        if (scopedSchoolIds.Count == 0) return false;

        return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profileId && scopedSchoolIds.Contains(x.SchoolId), cancellationToken);
    }

    private UpdateMyProfileRequest NormalizeSelfRequest(UserProfile profile, UpdateMyProfileRequest request)
    {
        var isStudentOnly = User.IsInRole("Student")
            && !User.IsInRole("Teacher")
            && !User.IsInRole("Parent")
            && !User.IsInRole("SchoolAdministrator")
            && !User.IsInRole("PlatformAdministrator");

        var canEditName = !isStudentOnly;
        var canEditPositionTitle = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Teacher");
        var canEditPublicContactNote = User.IsInRole("Teacher");
        var canEditPreferredContactNote = User.IsInRole("Parent");

        return request with
        {
            FirstName = canEditName ? request.FirstName : profile.FirstName,
            LastName = canEditName ? request.LastName : profile.LastName,
            PositionTitle = canEditPositionTitle ? request.PositionTitle : profile.PositionTitle,
            PublicContactNote = canEditPublicContactNote ? request.PublicContactNote : profile.PublicContactNote,
            PreferredContactNote = canEditPreferredContactNote ? request.PreferredContactNote : profile.PreferredContactNote
        };
    }

    private UpdateAdminProfileRequest NormalizeAdminRequest(UpdateAdminProfileRequest request)
    {
        if (SchoolScope.IsPlatformAdministrator(User))
        {
            return request;
        }

        return request with
        {
            PublicContactNote = null,
            PreferredContactNote = null
        };
    }

    private static IReadOnlyCollection<string> CollectChangedFields(UserProfile profile, ProfileEditableValues request)
    {
        var changed = new List<string>();

        if (!string.Equals(profile.FirstName, request.FirstName, StringComparison.Ordinal)) changed.Add("firstName");
        if (!string.Equals(profile.LastName, request.LastName, StringComparison.Ordinal)) changed.Add("lastName");
        if (!string.Equals(profile.PreferredDisplayName, request.PreferredDisplayName, StringComparison.Ordinal)) changed.Add("preferredDisplayName");
        if (!string.Equals(profile.PreferredLanguage, request.PreferredLanguage, StringComparison.Ordinal)) changed.Add("preferredLanguage");
        if (!string.Equals(profile.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal)) changed.Add("phoneNumber");
        if (!string.Equals(profile.PositionTitle, request.PositionTitle, StringComparison.Ordinal)) changed.Add("positionTitle");
        if (!string.Equals(profile.PublicContactNote, request.PublicContactNote, StringComparison.Ordinal)) changed.Add("publicContactNote");
        if (!string.Equals(profile.PreferredContactNote, request.PreferredContactNote, StringComparison.Ordinal)) changed.Add("preferredContactNote");

        return changed;
    }

    private static UserProfileContract ToContract(UserProfile profile)
        => new(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.UserType,
            profile.Email,
            profile.IsActive,
            profile.PreferredDisplayName,
            profile.PreferredLanguage,
            profile.PhoneNumber,
            profile.PositionTitle,
            profile.PublicContactNote,
            profile.PreferredContactNote);

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);

    public sealed record UpdateMyProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, PositionTitle, PublicContactNote, PreferredContactNote);

    public sealed record UpdateAdminProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, PositionTitle, PublicContactNote, PreferredContactNote);

    public abstract record ProfileEditableValues(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote);

    public sealed record SetActivationRequest(bool IsActive);
    public sealed record StudentContextContract(UserProfileContract Profile, IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments);

    public sealed record MyProfileSummaryContract(
        UserProfileContract Profile,
        IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments,
        IReadOnlyCollection<ParentStudentLinkContract> ParentStudentLinks,
        IReadOnlyCollection<Guid> SchoolIds,
        bool IsPlatformAdministrator,
        bool IsSchoolAdministrator,
        bool IsTeacher,
        bool IsParent,
        bool IsStudent);
}
