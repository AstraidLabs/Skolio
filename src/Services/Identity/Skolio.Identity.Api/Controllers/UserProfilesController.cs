using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserProfileContract>> Upsert([FromBody] UpsertUserProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpsertUserProfileCommand(request.UserProfileId, request.FirstName, request.LastName, request.UserType, request.Email), cancellationToken);
        return CreatedAtAction(nameof(Upsert), new { id = result.Id }, result);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);
}
