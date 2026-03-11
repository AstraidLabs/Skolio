using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/bootstrap")]
public sealed class BootstrapController(
    UserManager<SkolioIdentityUser> userManager,
    IdentityDbContext dbContext,
    IIdentityEmailSender identityEmailSender,
    IOptions<BootstrapOptions> bootstrapOptions,
    ILogger<BootstrapController> logger) : ControllerBase
{
    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<BootstrapAvailabilityResponse>> Availability(CancellationToken cancellationToken)
    {
        var status = await ResolveStatus(cancellationToken);
        Audit("identity.bootstrap.availability.decided", status.BootstrapUserId ?? "none", new { status = status.State });
        return Ok(status);
    }

    [HttpPost("platform-admin/create")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-reset-password")]
    public async Task<ActionResult<BootstrapAvailabilityResponse>> CreateFirstPlatformAdministrator([FromBody] CreateFirstPlatformAdministratorRequest request, CancellationToken cancellationToken)
    {
        var status = await ResolveStatus(cancellationToken);
        if (!string.Equals(status.State, BootstrapState.BootstrapAvailable, StringComparison.Ordinal))
        {
            return Conflict(status);
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return this.ValidationField("confirmPassword", "Password confirmation does not match.");
        }

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim();
        var now = DateTimeOffset.UtcNow;
        var user = new SkolioIdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            EmailConfirmed = false,
            AccountLifecycleStatus = IdentityAccountLifecycleStatus.PendingActivation,
            ActivationRequestedAtUtc = now,
            IsBootstrapPlatformAdministrator = true,
            InviteStatus = IdentityInviteStatus.PendingActivation
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(ToValidationProblem(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "PlatformAdministrator");
        if (!roleResult.Succeeded)
        {
            return BadRequest(ToValidationProblem(roleResult));
        }

        var profile = UserProfile.Create(
            Guid.Parse(user.Id),
            firstName: normalizedUserName,
            lastName: "PlatformAdministrator",
            userType: UserType.SupportStaff,
            email: normalizedEmail,
            preferredDisplayName: normalizedUserName,
            preferredLanguage: "cs",
            administrativeWorkDesignation: "PlatformAdministrator",
            platformRoleContextSummary: "Bootstrap first platform administrator");

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var activationUrl = BuildFrontendUrl($"/security/confirm-activation?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}");
        await identityEmailSender.SendAccountConfirmationAsync(new AccountConfirmationDelivery(normalizedEmail, normalizedUserName, activationUrl), cancellationToken);

        Audit("identity.bootstrap.platform-admin.created", user.Id, new { emailMasked = MaskEmail(normalizedEmail), userName = normalizedUserName });
        return Ok(await ResolveStatus(cancellationToken));
    }

    [HttpPost("platform-admin/mfa/setup/start")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<BootstrapMfaSetupStartResponse>> StartBootstrapMfaSetup([FromBody] BootstrapMfaSetupStartRequest request, CancellationToken cancellationToken)
    {
        var user = await ValidateBootstrapCredentials(request.UserId, request.Password, cancellationToken);
        if (user is null) return this.ValidationForm("Bootstrap account credentials are invalid.");

        await userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var sharedKey = FormatKey(unformattedKey);
        var issuer = "Skolio";
        var accountLabel = user.Email ?? user.UserName ?? user.Id;
        var uri = $"otpauth://totp/{Uri.EscapeDataString($"{issuer}:{accountLabel}")}?secret={Uri.EscapeDataString(unformattedKey)}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

        Audit("identity.bootstrap.mfa.setup.started", user.Id, new { user = user.UserName });
        return Ok(new BootstrapMfaSetupStartResponse(sharedKey, uri, issuer, accountLabel));
    }

    [HttpPost("platform-admin/mfa/setup/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<BootstrapMfaSetupConfirmResponse>> ConfirmBootstrapMfaSetup([FromBody] BootstrapMfaSetupConfirmRequest request, CancellationToken cancellationToken)
    {
        var user = await ValidateBootstrapCredentials(request.UserId, request.Password, cancellationToken);
        if (user is null) return this.ValidationForm("Bootstrap account credentials are invalid.");

        var verificationCode = NormalizeCode(request.VerificationCode);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
        if (!isValid) return this.ValidationField("verificationCode", "Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();
        user.BootstrapMfaCompletedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        Audit("identity.bootstrap.mfa.setup.completed", user.Id, new { recoveryCodesCount = recoveryCodes.Length });
        return Ok(new BootstrapMfaSetupConfirmResponse(recoveryCodes));
    }

    [HttpGet("state")]
    [AllowAnonymous]
    public async Task<ActionResult<BootstrapAvailabilityResponse>> State(CancellationToken cancellationToken)
    {
        var status = await ResolveStatus(cancellationToken);
        return Ok(status);
    }

    private async Task<SkolioIdentityUser?> ValidateBootstrapCredentials(string userId, string password, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var status = await ResolveStatus(cancellationToken);
        if (!string.Equals(status.BootstrapUserId, userId, StringComparison.Ordinal)) return null;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsBootstrapPlatformAdministrator) return null;
        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid) return null;
        return user;
    }

    private async Task<BootstrapAvailabilityResponse> ResolveStatus(CancellationToken cancellationToken)
    {
        var hasActivePlatformAdministrator = await HasActivePlatformAdministrator(cancellationToken);
        if (hasActivePlatformAdministrator)
        {
            return new BootstrapAvailabilityResponse(BootstrapState.BootstrapClosed, false, null);
        }

        var bootstrapUser = await dbContext.Users
            .Where(x => x.IsBootstrapPlatformAdministrator)
            .OrderByDescending(x => x.ActivationRequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (!IsBootstrapEnabled())
        {
            return new BootstrapAvailabilityResponse(BootstrapState.BootstrapClosed, false, bootstrapUser?.Id);
        }

        if (bootstrapUser is null)
        {
            return new BootstrapAvailabilityResponse(BootstrapState.BootstrapAvailable, true, null);
        }

        if (bootstrapUser.BootstrapFirstLoginCompletedAtUtc.HasValue)
        {
            return new BootstrapAvailabilityResponse(BootstrapState.Active, false, bootstrapUser.Id);
        }

        if (!bootstrapUser.BootstrapMfaCompletedAtUtc.HasValue)
        {
            return new BootstrapAvailabilityResponse(BootstrapState.BootstrapAccountCreated, true, bootstrapUser.Id);
        }

        if (!bootstrapUser.EmailConfirmed || !bootstrapUser.BootstrapActivationCompletedAtUtc.HasValue)
        {
            return new BootstrapAvailabilityResponse(BootstrapState.PendingActivation, true, bootstrapUser.Id);
        }

        return new BootstrapAvailabilityResponse(BootstrapState.PendingFirstLogin, true, bootstrapUser.Id);
    }


    private bool IsBootstrapEnabled()
    {
        var env = Environment.GetEnvironmentVariable("BOOTSTRAP_PLATFORM_ADMIN_ENABLED");
        if (bool.TryParse(env, out var enabledFromEnv)) return enabledFromEnv;
        return bootstrapOptions.Value.PlatformAdminEnabled;
    }

    private async Task<bool> HasActivePlatformAdministrator(CancellationToken cancellationToken)
    {
        var activeAdminIds = from user in dbContext.Users
                             join ur in dbContext.UserRoles on user.Id equals ur.UserId
                             join role in dbContext.Roles on ur.RoleId equals role.Id
                             where role.Name == "PlatformAdministrator"
                                   && user.AccountLifecycleStatus == IdentityAccountLifecycleStatus.Active
                                   && user.EmailConfirmed
                             select user.Id;

        return await activeAdminIds.AnyAsync(cancellationToken);
    }

    private string BuildFrontendUrl(string pathAndQuery)
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin)) return $"{origin.TrimEnd('/')}{pathAndQuery}";
        return $"http://localhost:8080{pathAndQuery}";
    }

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private static string FormatKey(string unformattedKey)
    {
        if (string.IsNullOrWhiteSpace(unformattedKey)) return string.Empty;
        var builder = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i++)
        {
            if (i > 0 && i % 4 == 0) builder.Append(' ');
            builder.Append(char.ToLowerInvariant(unformattedKey[i]));
        }

        return builder.ToString();
    }

    private static ValidationProblemDetails ToValidationProblem(IdentityResult result)
    {
        var details = new ValidationProblemDetails
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        };

        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Code) ? "form" : error.Code;
            if (details.Errors.TryGetValue(key, out var existing))
            {
                details.Errors[key] = [.. existing, error.Description];
            }
            else
            {
                details.Errors.Add(key, [error.Description]);
            }
        }

        return details;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "***";
        var parts = email.Split('@');
        var local = parts[0];
        var domain = parts[1];
        if (local.Length <= 2) return $"{local[0]}***@{domain}";
        return $"{local[..2]}***@{domain}";
    }

    private void Audit(string actionCode, string targetId, object payload) => logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, "anonymous", targetId, payload);
}

public static class BootstrapState
{
    public const string BootstrapAvailable = "BootstrapAvailable";
    public const string BootstrapAccountCreated = "BootstrapAccountCreated";
    public const string PendingActivation = "PendingActivation";
    public const string PendingFirstLogin = "PendingFirstLogin";
    public const string Active = "Active";
    public const string BootstrapClosed = "BootstrapClosed";
}

public sealed record BootstrapAvailabilityResponse(string State, bool Allowed, string? BootstrapUserId);
public sealed record BootstrapMfaSetupStartResponse(string SharedKey, string AuthenticatorUri, string Issuer, string AccountLabel);
public sealed record BootstrapMfaSetupConfirmResponse(string[] RecoveryCodes);

public sealed class CreateFirstPlatformAdministratorRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class BootstrapMfaSetupStartRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class BootstrapMfaSetupConfirmRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string VerificationCode { get; init; } = string.Empty;
}
