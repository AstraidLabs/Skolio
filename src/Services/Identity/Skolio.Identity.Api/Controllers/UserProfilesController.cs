using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator, IdentityDbContext dbContext) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileContract>> Me(CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(cancellationToken);
        return profile is null ? NotFound() : Ok(new UserProfileContract(profile.Id, profile.FirstName, profile.LastName, profile.UserType, profile.Email));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileContract>> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is null) return NotFound();
        var result = await mediator.Send(new UpsertUserProfileCommand(profile.Id, request.FirstName, request.LastName, request.UserType, request.Email), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileContract>> Upsert([FromBody] UpsertUserProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpsertUserProfileCommand(request.UserProfileId, request.FirstName, request.LastName, request.UserType, request.Email), cancellationToken);
        return CreatedAtAction(nameof(Upsert), new { id = result.Id }, result);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);
    public sealed record UpdateMyProfileRequest(string FirstName, string LastName, UserType UserType, string Email);
}
