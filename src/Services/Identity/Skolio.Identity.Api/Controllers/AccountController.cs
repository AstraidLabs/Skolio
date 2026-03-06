using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("account")]
public sealed class AccountController(SignInManager<SkolioIdentityUser> signInManager) : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public ContentResult LoginPage([FromQuery] string? returnUrl)
    {
        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var html = $"""
                    <html><body>
                    <h1>Skolio Sign-In</h1>
                    <form method='post' action='/account/login'>
                      <input type='hidden' name='returnUrl' value='{safeReturnUrl}' />
                      <label>User</label><input type='text' name='username' />
                      <label>Password</label><input type='password' name='password' />
                      <button type='submit'>Sign in</button>
                    </form>
                    </body></html>
                    """;

        return Content(html, "text/html");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] string returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(username, password, true, false);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
