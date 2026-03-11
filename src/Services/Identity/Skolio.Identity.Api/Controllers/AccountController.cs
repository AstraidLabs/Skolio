using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using Skolio.Identity.Domain.Enums;
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
    private const string LoginAttemptKeyPrefix = "identity:login:attempt:";
    private const string LoginCaptchaKeyPrefix = "identity:login:captcha:";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LoginAttemptLifetime = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan CaptchaLifetime = TimeSpan.FromMinutes(5);
    private const int MaxChallengeAttempts = 6;
    private const int CaptchaTriggerFailedAttempts = 2;

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult LoginPage([FromQuery] string? returnUrl)
    {
        var safeReturnUrl = SafeReturnUrl(returnUrl);
        return Redirect(BuildFrontendLoginUrl(safeReturnUrl));
    }

    [HttpGet("login/captcha")]
    [AllowAnonymous]
    public IActionResult LoginCaptcha([FromQuery] string captchaId)
    {
        if (!TryGetCaptchaChallenge(captchaId, out var challenge) || challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return NotFound();
        }

        var svg = BuildCaptchaSvg(challenge.Code);
        return Content(svg, "image/svg+xml", Encoding.UTF8);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-login-primary")]
    public async Task<IActionResult> Login(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] string returnUrl,
        [FromForm] bool rememberMe,
        [FromForm] string? captchaId,
        [FromForm] string? captchaAnswer,
        CancellationToken cancellationToken)
    {
        var safeReturnUrl = SafeReturnUrl(returnUrl);
        var attemptKey = BuildLoginAttemptKey(username);
        var failedAttempts = GetFailedAttemptCount(attemptKey);
        var captchaRequired = failedAttempts >= CaptchaTriggerFailedAttempts;

        if (captchaRequired)
        {
            if (!ValidateCaptcha(attemptKey, captchaId, captchaAnswer))
            {
                var nextFailedAttempts = IncrementFailedAttempt(attemptKey);
                var captchaChallengeId = EnsureCaptchaChallenge(attemptKey, captchaId);
                var captchaError = string.IsNullOrWhiteSpace(captchaAnswer) ? "login_captcha_required" : "login_captcha_invalid";
                Audit("identity.auth.login.captcha.failed", "unknown", new { reason = captchaError, failedAttempts = nextFailedAttempts });
                return Redirect(BuildFrontendLoginUrl(
                    safeReturnUrl,
                    ("error", captchaError),
                    ("captcha", "required"),
                    ("captchaId", captchaChallengeId)));
            }
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            var nextFailedAttempts = IncrementFailedAttempt(attemptKey);
            return Redirect(BuildFrontendLoginUrl(
                safeReturnUrl,
                ("error", "login_invalid_credentials"),
                ("captcha", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? "required" : "optional"),
                ("captchaId", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? CreateCaptchaChallenge(attemptKey) : string.Empty)));
        }

        var user = await ResolveUser(username.Trim(), cancellationToken);
        if (user is null)
        {
            var nextFailedAttempts = IncrementFailedAttempt(attemptKey);
            Audit("identity.auth.login.primary.failed", "unknown", new { reason = "invalid-credentials" });
            return Redirect(BuildFrontendLoginUrl(
                safeReturnUrl,
                ("error", "login_invalid_credentials"),
                ("captcha", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? "required" : "optional"),
                ("captchaId", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? CreateCaptchaChallenge(attemptKey) : string.Empty)));
        }

        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Deactivated)
        {
            Audit("identity.auth.login.primary.blocked", user.Id, new { reason = "deactivated" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_account_deactivated")));
        }

        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.PendingActivation || !user.EmailConfirmed)
        {
            Audit("identity.auth.login.primary.blocked", user.Id, new { reason = "pending-activation" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_activation_required")));
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (passwordResult.IsLockedOut)
        {
            user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Locked;
            await userManager.UpdateAsync(user);
            Audit("identity.auth.login.primary.locked", user.Id, new { reason = "identity-lockout" });
            return Redirect(BuildFrontendLoginUrl(safeReturnUrl, ("error", "login_account_locked")));
        }
        if (!passwordResult.Succeeded)
        {
            var nextFailedAttempts = IncrementFailedAttempt(attemptKey);
            Audit("identity.auth.login.primary.failed", user.Id, new { reason = "invalid-credentials" });
            return Redirect(BuildFrontendLoginUrl(
                safeReturnUrl,
                ("error", "login_invalid_credentials"),
                ("captcha", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? "required" : "optional"),
                ("captchaId", nextFailedAttempts >= CaptchaTriggerFailedAttempts ? CreateCaptchaChallenge(attemptKey) : string.Empty)));
        }

        ClearFailedAttempts(attemptKey);

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            await signInManager.SignInAsync(user, isPersistent: rememberMe);
            user.LastLoginAtUtc = DateTimeOffset.UtcNow;
            user.LastActivityAtUtc = DateTimeOffset.UtcNow;
            if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Locked) user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
            await userManager.UpdateAsync(user);
            Audit("identity.auth.login.primary.succeeded", user.Id, new { mfaRequired = false, rememberMe });
            return LocalRedirect(safeReturnUrl);
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime);
        challengeCache.Set(CacheKey(challengeId), new MfaLoginChallenge(user.Id, safeReturnUrl, expiresAt, 0, rememberMe), expiresAt);

        Audit("identity.auth.login.mfa.required", user.Id, new { challengeId, expiresAtUtc = expiresAt.ToString("O"), rememberMe });
        return Redirect(BuildFrontendLoginUrl(
            safeReturnUrl,
            ("mfa", "required"),
            ("challengeId", challengeId),
            ("expiresAtUtc", expiresAt.ToString("O")),
            ("rememberMe", rememberMe ? "true" : "false")));
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
            return Redirect(BuildFrontendLoginUrl(
                safeReturnUrl,
                ("mfa", "required"),
                ("challengeId", challengeId),
                ("rememberMe", challenge.RememberMe ? "true" : "false"),
                ("error", useRecoveryCode ? "login_mfa_invalid_recovery_code" : "login_mfa_invalid_code")));
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
                ("rememberMe", challenge.RememberMe ? "true" : "false"),
                ("error", useRecoveryCode ? "login_mfa_invalid_recovery_code" : "login_mfa_invalid_code")));
        }

        challengeCache.Remove(CacheKey(challengeId));
        await signInManager.SignInAsync(user, isPersistent: challenge.RememberMe);
        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.LastActivityAtUtc = DateTimeOffset.UtcNow;
        if (user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Locked) user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
        if (user.IsBootstrapPlatformAdministrator && user.BootstrapFirstLoginCompletedAtUtc is null && user.EmailConfirmed && await userManager.GetTwoFactorEnabledAsync(user))
        {
            user.BootstrapFirstLoginCompletedAtUtc = DateTimeOffset.UtcNow;
            Audit("identity.bootstrap.first-login.completed", user.Id, new { action = "bootstrap-first-login" });
            Audit("identity.bootstrap.closed", user.Id, new { action = "bootstrap-closed" });
        }

        await userManager.UpdateAsync(user);
        Audit(useRecoveryCode ? "identity.auth.login.mfa.recovery-code.succeeded" : "identity.auth.login.mfa.succeeded", user.Id, new { challengeId, rememberMe = challenge.RememberMe });
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

    private string BuildLoginAttemptKey(string username)
    {
        var normalizedUsername = string.IsNullOrWhiteSpace(username) ? "unknown" : username.Trim().ToLowerInvariant();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return $"{LoginAttemptKeyPrefix}{ip}:{normalizedUsername}";
    }

    private int GetFailedAttemptCount(string attemptKey)
    {
        if (challengeCache.TryGetValue<LoginAttemptState>(attemptKey, out var state) && state is not null)
        {
            return state.FailedCount;
        }

        return 0;
    }

    private int IncrementFailedAttempt(string attemptKey)
    {
        var nextCount = GetFailedAttemptCount(attemptKey) + 1;
        challengeCache.Set(attemptKey, new LoginAttemptState(nextCount, DateTimeOffset.UtcNow), LoginAttemptLifetime);
        return nextCount;
    }

    private void ClearFailedAttempts(string attemptKey)
    {
        challengeCache.Remove(attemptKey);
    }

    private string EnsureCaptchaChallenge(string attemptKey, string? existingCaptchaId)
    {
        if (!string.IsNullOrWhiteSpace(existingCaptchaId) && TryGetCaptchaChallenge(existingCaptchaId, out var existingChallenge))
        {
            if (existingChallenge.AttemptKey == attemptKey && existingChallenge.ExpiresAtUtc > DateTimeOffset.UtcNow)
            {
                return existingCaptchaId;
            }
        }

        return CreateCaptchaChallenge(attemptKey);
    }

    private bool ValidateCaptcha(string attemptKey, string? captchaId, string? captchaAnswer)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(captchaAnswer))
        {
            return false;
        }

        if (!TryGetCaptchaChallenge(captchaId, out var challenge))
        {
            return false;
        }

        if (challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow || challenge.AttemptKey != attemptKey)
        {
            challengeCache.Remove($"{LoginCaptchaKeyPrefix}{captchaId}");
            return false;
        }

        var normalizedAnswer = NormalizeCaptcha(captchaAnswer);
        var valid = string.Equals(normalizedAnswer, challenge.Code, StringComparison.Ordinal);
        if (valid)
        {
            challengeCache.Remove($"{LoginCaptchaKeyPrefix}{captchaId}");
        }

        return valid;
    }

    private string CreateCaptchaChallenge(string attemptKey)
    {
        var captchaId = Guid.NewGuid().ToString("N");
        var code = GenerateCaptchaCode(5);
        var expiresAt = DateTimeOffset.UtcNow.Add(CaptchaLifetime);
        challengeCache.Set($"{LoginCaptchaKeyPrefix}{captchaId}", new LoginCaptchaChallenge(code, attemptKey, expiresAt), expiresAt);
        return captchaId;
    }

    private bool TryGetCaptchaChallenge(string captchaId, out LoginCaptchaChallenge challenge)
    {
        if (challengeCache.TryGetValue<LoginCaptchaChallenge>($"{LoginCaptchaKeyPrefix}{captchaId}", out var found) && found is not null)
        {
            challenge = found;
            return true;
        }

        challenge = default!;
        return false;
    }

    private static string GenerateCaptchaCode(int length)
    {
        const string alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(buffer);
    }

    private static string NormalizeCaptcha(string value)
    {
        return value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static string BuildCaptchaSvg(string code)
    {
        var escapedCode = System.Net.WebUtility.HtmlEncode(code);
        return $"""
        <svg xmlns='http://www.w3.org/2000/svg' width='180' height='56' viewBox='0 0 180 56'>
          <defs>
            <linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
              <stop offset='0%' stop-color='#eff6ff'/>
              <stop offset='100%' stop-color='#dbeafe'/>
            </linearGradient>
          </defs>
          <rect width='180' height='56' rx='10' fill='url(#g)'/>
          <path d='M8 42 C36 10, 74 52, 112 20 S168 38, 176 12' stroke='#93c5fd' stroke-width='2' fill='none' opacity='0.55'/>
          <path d='M6 16 C40 40, 78 8, 116 34 S156 22, 176 44' stroke='#60a5fa' stroke-width='1.4' fill='none' opacity='0.45'/>
          <text x='90' y='36' text-anchor='middle' font-family='Segoe UI, Arial, sans-serif' font-size='26' font-weight='700' letter-spacing='4' fill='#1e3a8a'>{escapedCode}</text>
        </svg>
        """;
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

    private sealed record MfaLoginChallenge(string UserId, string ReturnUrl, DateTimeOffset ExpiresAtUtc, int Attempts, bool RememberMe);
    private sealed record LoginAttemptState(int FailedCount, DateTimeOffset LastFailedAtUtc);
    private sealed record LoginCaptchaChallenge(string Code, string AttemptKey, DateTimeOffset ExpiresAtUtc);
}
