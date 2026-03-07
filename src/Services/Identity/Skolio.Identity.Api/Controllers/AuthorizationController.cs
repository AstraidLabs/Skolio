using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Extensions;
using OpenIddict.Server.AspNetCore;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
public sealed class AuthorizationController(SignInManager<SkolioIdentityUser> signInManager, UserManager<SkolioIdentityUser> userManager) : ControllerBase
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
            role = claimsPrincipal.FindAll(ClaimTypes.Role).Select(roleClaim => roleClaim.Value).ToArray()
        });
    }

    [HttpGet("/connect/logout")]
    [HttpPost("/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
