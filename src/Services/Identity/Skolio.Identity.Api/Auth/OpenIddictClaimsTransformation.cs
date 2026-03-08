using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Skolio.Identity.Api.Auth;

public sealed class OpenIddictClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        foreach (var claim in identity.FindAll("role").ToList())
        {
            if (!identity.HasClaim(ClaimTypes.Role, claim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
            }
        }

        return Task.FromResult(principal);
    }
}
