using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator, IdentityDbContext dbContext, ILogger<UserProfilesController> logger) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<UserProfileContract>> Me(CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(cancellationToken);
        return profile is null ? NotFound() : Ok(new UserProfileContract(profile.Id, profile.FirstName, profile.LastName, profile.UserType, profile.Email, profile.IsActive));
    }

    [HttpPut("me")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<UserProfileContract>> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is null) return NotFound();

        var result = await mediator.Send(new UpsertUserProfileCommand(profile.Id, request.FirstName, request.LastName, request.UserType, request.Email), cancellationToken);
        return Ok(result);
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
            .Select(x => new UserProfileContract(x.Id, x.FirstName, x.LastName, x.UserType, x.Email, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return profile is null ? NotFound() : Ok(new UserProfileContract(profile.Id, profile.FirstName, profile.LastName, profile.UserType, profile.Email, profile.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Update(Guid id, [FromBody] UpdateAdminProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        var result = await mediator.Send(new UpsertUserProfileCommand(profile.Id, request.FirstName, request.LastName, request.UserType, request.Email), cancellationToken);
        Audit("identity.user-profile.updated", id, new { request.UserType, request.Email });
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
        return Ok(new UserProfileContract(profile.Id, profile.FirstName, profile.LastName, profile.UserType, profile.Email, profile.IsActive));
    }

    private async Task<bool> HasProfileAccess(Guid profileId, CancellationToken cancellationToken)
    {
        if (SchoolScope.IsPlatformAdministrator(User)) return true;

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
        if (scopedSchoolIds.Count == 0) return false;

        return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profileId && scopedSchoolIds.Contains(x.SchoolId), cancellationToken);
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);
    public sealed record UpdateMyProfileRequest(string FirstName, string LastName, UserType UserType, string Email);
    public sealed record UpdateAdminProfileRequest(string FirstName, string LastName, UserType UserType, string Email);
    public sealed record SetActivationRequest(bool IsActive);
}
