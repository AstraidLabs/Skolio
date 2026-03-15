using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Skolio.Academics.Application.Abstractions;

namespace Skolio.Academics.Infrastructure.Auth;

public sealed class ClaimsCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context available.");

    public string UserId =>
        User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Missing sub claim.");

    public bool IsPlatformAdministrator => User.IsInRole("PlatformAdministrator");

    public bool HasAccessToSchool(Guid schoolId)
    {
        if (IsPlatformAdministrator) return true;

        return User.FindAll("school_id")
            .Any(c => Guid.TryParse(c.Value, out var id) && id == schoolId);
    }
}
