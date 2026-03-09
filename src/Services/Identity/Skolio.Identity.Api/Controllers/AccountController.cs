using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("account")]
public sealed class AccountController(
    SignInManager<SkolioIdentityUser> signInManager,
    UserManager<SkolioIdentityUser> userManager,
    IMemoryCache challengeCache,
    ILogger<AccountController> logger) : ControllerBase
{
    private const string ChallengeKeyPrefix = "identity:login:mfa:";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private const int MaxChallengeAttempts = 6;

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult LoginPage([FromQuery] string? returnUrl)
    {
        var safeReturnUrl = SafeReturnUrl(returnUrl);
        return Redirect(BuildFrontendLoginUrl(safeReturnUrl));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-login-primary")]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] string returnUrl, CancellationToken cancellationToken)
    {
        var safeReturnUrl = SafeReturnUrl(returnUrl);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_invalid_credentials")));
        }

        var user = await ResolveUser(username.Trim(), cancellationToken);
        if (user is null)
        {
            Audit("identity.auth.login.primary.failed", "unknown", new { reason = "invalid-credentials" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_invalid_credentials")));
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!passwordResult.Succeeded)
        {
            Audit("identity.auth.login.primary.failed", user.Id, new { reason = "invalid-credentials" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_invalid_credentials")));
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            await signInManager.SignInAsync(user, isPersistent: true);
            Audit("identity.auth.login.primary.succeeded", user.Id, new { mfaRequired = false });
            return LocalRedirect(safeReturnUrl);
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime);
        challengeCache.Set(CacheKey(challengeId), new MfaLoginChallenge(user.Id, safeReturnUrl, expiresAt, 0), expiresAt);

        Audit("identity.auth.login.mfa.required", user.Id, new { challengeId, expiresAtUtc = expiresAt.ToString("O") });
        return Redirect(BuildFrontendLoginUrl(
            safeReturnUrl,
            ("mfa", "required"),
            ("challengeId", challengeId),
            ("expiresAtUtc", expiresAt.ToString("O"))));
    }

    [HttpPost("login/mfa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-login-mfa-challenge")]
    public async Task<IActionResult> VerifyMfa(
        [FromForm] string challengeId,
        [FromForm] string returnUrl,
        [FromForm] string code,
        [FromForm] bool useRecoveryCode,
        CancellationToken cancellationToken)
    {
        var safeReturnUrl = SafeReturnUrl(returnUrl);
        if (string.IsNullOrWhiteSpace(challengeId))
        {
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_mfa_challenge_expired")));
        }

        if (!challengeCache.TryGetValue<MfaLoginChallenge>(CacheKey(challengeId), out var challenge) || challenge is null)
        {
            Audit("identity.auth.login.mfa.failed", "unknown", new { challengeId, reason = "challenge-not-found" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_mfa_challenge_expired")));
        }

        if (challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            challengeCache.Remove(CacheKey(challengeId));
            Audit("identity.auth.login.mfa.challenge.expired", challenge.UserId, new { challengeId });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_mfa_challenge_expired")));
        }

        var user = await userManager.FindByIdAsync(challenge.UserId);
        if (user is null)
        {
            challengeCache.Remove(CacheKey(challengeId));
            Audit("identity.auth.login.mfa.failed", challenge.UserId, new { challengeId, reason = "user-not-found" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_mfa_challenge_expired")));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("mfa", "required"), ("challengeId", challengeId), ("error", useRecoveryCode ? "login_mfa_invalid_recovery_code" : "login_mfa_invalid_code")));
        }

        var valid = useRecoveryCode
            ? (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, NormalizeCode(code))).Succeeded
            : await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, NormalizeCode(code));

        if (!valid)
        {
            var attempts = challenge.Attempts + 1;
            if (attempts >= MaxChallengeAttempts)
            {
                challengeCache.Remove(CacheKey(challengeId));
                Audit("identity.auth.login.mfa.failed", user.Id, new { challengeId, reason = "too-many-attempts", usedRecoveryCode = useRecoveryCode });
                return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_mfa_blocked")));
            }

            challengeCache.Set(CacheKey(challengeId), challenge with { Attempts = attempts }, challenge.ExpiresAtUtc);
            Audit("identity.auth.login.mfa.failed", user.Id, new { challengeId, reason = "invalid-code", usedRecoveryCode = useRecoveryCode, attempts });
            return Redirect(BuildFrontendLoginUrl(
                safeReturnUrl,
                ("mfa", "required"),
                ("challengeId", challengeId),
                ("expiresAtUtc", challenge.ExpiresAtUtc.ToString("O")),
                ("error", useRecoveryCode ? "login_mfa_invalid_recovery_code" : "login_mfa_invalid_code")));
        }

        challengeCache.Remove(CacheKey(challengeId));
        await signInManager.SignInAsync(user, isPersistent: true);
        Audit(useRecoveryCode ? "identity.auth.login.mfa.recovery-code.succeeded" : "identity.auth.login.mfa.succeeded", user.Id, new { challengeId });
        return LocalRedirect(challenge.ReturnUrl);
    }

    private async Task<SkolioIdentityUser?> ResolveUser(string usernameOrEmail, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (usernameOrEmail.Contains('@', StringComparison.Ordinal))
        {
            var byEmail = await userManager.FindByEmailAsync(usernameOrEmail);
            if (byEmail is not null) return byEmail;
        }

        return await userManager.FindByNameAsync(usernameOrEmail);
    }

    private string BuildFrontendLoginUrl(string returnUrl, params (string Key, string Value)[] additionalParams)
    {
        var baseUrl = ResolveFrontendLoginBaseUrl(returnUrl);
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["returnUrl"] = returnUrl
        };

        foreach (var (key, value) in additionalParams)
        {
            query[key] = value;
        }

        return QueryHelpers.AddQueryString(baseUrl, query);
    }

    private string ResolveFrontendLoginBaseUrl(string returnUrl)
    {
        var queryStart = returnUrl.IndexOf('?', StringComparison.Ordinal);
        if (queryStart >= 0 && queryStart + 1 < returnUrl.Length)
        {
            var query = QueryHelpers.ParseQuery(returnUrl[(queryStart + 1)..]);
            if (query.TryGetValue("redirect_uri", out var redirectUriRaw)
                && Uri.TryCreate(redirectUriRaw.FirstOrDefault(), UriKind.Absolute, out var redirectUri))
            {
                return $"{redirectUri.GetLeftPart(UriPartial.Authority)}/login";
            }
        }

        var origin = Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return $"{origin.TrimEnd('/')}/login";
        }

        return "/login";
    }

    private static string CacheKey(string challengeId) => $"{ChallengeKeyPrefix}{challengeId}";

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return "/";
        return returnUrl.StartsWith("/", StringComparison.Ordinal) ? returnUrl : "/";
    }

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private void Audit(string actionCode, string targetId, object payload)
    {
        var actor = User?.Identity?.Name ?? "anonymous";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    private sealed record MfaLoginChallenge(string UserId, string ReturnUrl, DateTimeOffset ExpiresAtUtc, int Attempts);
}
