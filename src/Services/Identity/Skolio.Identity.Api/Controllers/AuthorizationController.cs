using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Extensions;
using OpenIddict.Server.AspNetCore;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
public sealed class AuthorizationController(
    SignInManager<SkolioIdentityUser> signInManager,
    UserManager<SkolioIdentityUser> userManager,
    IdentityDbContext dbContext,
    ILogger<AuthorizationController> logger) : ControllerBase
{
    [HttpGet("/connect/authorize")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict request missing.");
        var returnUrl = $"{Request.Path}{Request.QueryString}";

        var loginBaseUrl = "/account/login";
        if (Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out var redirectUri))
        {
            loginBaseUrl = $"{redirectUri.GetLeftPart(UriPartial.Authority)}/login";
        }

        var loginUrl = $"{loginBaseUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}";

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Redirect(loginUrl);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Redirect(loginUrl);
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);
        principal.SetClaim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty);

        if (Guid.TryParse(user.Id, out var userProfileId))
        {
            var roleAssignments = dbContext.SchoolRoleAssignments.Where(x => x.UserProfileId == userProfileId).ToList();
            foreach (var assignment in roleAssignments)
            {
                ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.Role, assignment.RoleCode));
                ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("school_id", assignment.SchoolId.ToString()));
            }

            var linkedStudentIds = await dbContext.ParentStudentLinks
                .Where(x => x.ParentUserProfileId == userProfileId)
                .Select(x => x.StudentUserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var linkedStudentId in linkedStudentIds)
            {
                ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("linked_student_id", linkedStudentId.ToString()));
            }
        }

        principal.SetScopes(request.GetScopes());
        principal.SetResources("skolio_api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("/connect/token")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict request missing.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    [HttpGet("/connect/userinfo")]
    [HttpPost("/connect/userinfo")]
    public IActionResult UserInfo()
    {
        var claimsPrincipal = User;
        return Ok(new
        {
            sub = claimsPrincipal.FindFirstValue(OpenIddictConstants.Claims.Subject),
            email = claimsPrincipal.FindFirstValue(OpenIddictConstants.Claims.Email),
            role = claimsPrincipal.FindAll(ClaimTypes.Role).Select(roleClaim => roleClaim.Value).ToArray(),
            school_id = claimsPrincipal.FindAll("school_id").Select(x => x.Value).ToArray(),
            linked_student_id = claimsPrincipal.FindAll("linked_student_id").Select(x => x.Value).ToArray()
        });
    }

    [HttpGet("/connect/logout")]
    [HttpPost("/connect/logout")]
    public async Task<IActionResult> Logout([FromQuery] string? logoutReason = null)
    {
        var normalizedReason = string.Equals(logoutReason, "idle", StringComparison.OrdinalIgnoreCase) ? "idle" : "manual";
        var actor = User.FindFirstValue(OpenIddictConstants.Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={Target} payload={Payload}",
            normalizedReason == "idle" ? "identity.auth.logout.idle-timeout" : "identity.auth.logout.user-initiated",
            actor,
            actor,
            new { reason = normalizedReason });

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
