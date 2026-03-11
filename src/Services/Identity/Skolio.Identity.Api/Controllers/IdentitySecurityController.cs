using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/security")]
public sealed class IdentitySecurityController(
    UserManager<SkolioIdentityUser> userManager,
    SignInManager<SkolioIdentityUser> signInManager,
    IIdentityEmailSender identityEmailSender,
    ILogger<IdentitySecurityController> logger) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize]
    public async Task<ActionResult<SecuritySummaryContract>> Summary(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
        var hasAuthenticatorKey = !string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(user));

        return Ok(new SecuritySummaryContract(
            user.Id,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            await userManager.GetTwoFactorEnabledAsync(user),
            hasAuthenticatorKey,
            recoveryCodesLeft));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal)) return this.ValidationField("confirmNewPassword", "Password confirmation does not match.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.RefreshSignInAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Password changed",
                "Your Skolio password was changed successfully."),
            cancellationToken);

        Audit("identity.security.password.changed", user.Id, new { action = "change-password" });
        return Ok(new { message = "Password changed." });
    }


    [HttpPost("activation/resend")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-forgot-password")]
    public async Task<IActionResult> ResendActivation([FromBody] ResendActivationRequest request, CancellationToken cancellationToken)
    {
        var genericResponse = Ok(new { message = "If the account exists, an activation email has been sent." });
        if (string.IsNullOrWhiteSpace(request.Email)) return genericResponse;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed) return genericResponse;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var activationUrl = BuildFrontendUrl($"/security/confirm-activation?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}");
        await identityEmailSender.SendAccountConfirmationAsync(new AccountConfirmationDelivery(user.Email ?? request.Email, BuildDisplayName(user), activationUrl), cancellationToken);

        Audit("identity.security.activation.resent", user.Id, new { action = "activation-resend" });
        return genericResponse;
    }

    [HttpPost("activation/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-reset-password")]
    public async Task<IActionResult> ConfirmActivation([FromBody] ConfirmActivationRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Activation token is invalid or expired.");

        var token = DecodeToken(request.Token);
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        user.AccountLifecycleStatus = Skolio.Identity.Domain.Enums.IdentityAccountLifecycleStatus.Active;
        user.ActivatedAtUtc = DateTimeOffset.UtcNow;
        user.DeactivatedAtUtc = null;
        user.DeactivationReason = null;
        await userManager.UpdateAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(new SecurityNotificationDelivery(user.Email ?? string.Empty, BuildDisplayName(user), "Account activated", "Your Skolio account activation is complete."), cancellationToken);
        Audit("identity.security.activation.confirmed", user.Id, new { action = "activation-confirm" });
        return Ok(new { message = "Account activated." });
    }

    [HttpGet("invite/context")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteContextContract>> InviteContext([FromQuery] string userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return this.ValidationForm("Invite is invalid or expired.");
        if (!IsInviteTokenValid(user, token)) return this.ValidationForm("Invite is invalid or expired.");
        if (user.InviteExpiresAtUtc is null || user.InviteExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            user.InviteStatus = Skolio.Identity.Domain.Enums.IdentityInviteStatus.InviteExpired;
            await userManager.UpdateAsync(user);
            return this.ValidationForm("Invite is invalid or expired.");
        }

        return Ok(new InviteContextContract(user.Id, MaskEmail(user.Email ?? string.Empty), user.InviteStatus.ToString(), user.InviteExpiresAtUtc));
    }

    [HttpPost("invite/confirm-code")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-invite-code")]
    public async Task<IActionResult> ConfirmInviteCode([FromBody] ConfirmInviteCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Invite is invalid or expired.");
        if (!IsInviteTokenValid(user, request.Token)) return this.ValidationForm("Invite is invalid or expired.");
        if (user.InviteExpiresAtUtc is null || user.InviteExpiresAtUtc <= DateTimeOffset.UtcNow) return this.ValidationForm("Invite is invalid or expired.");
        if (!VerifyHash(request.ActivationCode.Trim(), user.InviteCodeHash)) return this.ValidationField("activationCode", "Activation code is invalid.");

        user.InviteStatus = Skolio.Identity.Domain.Enums.IdentityInviteStatus.InviteConfirmed;
        user.InviteConfirmedAtUtc = DateTimeOffset.UtcNow;
        user.InviteCodeHash = null;
        await userManager.UpdateAsync(user);
        Audit("identity.security.invite.confirmed", user.Id, new { action = "invite-code-confirm" });
        return Ok(new { message = "Invite code confirmed." });
    }

    [HttpPost("invite/complete")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-reset-password")]
    public async Task<IActionResult> CompleteInvite([FromBody] CompleteInviteRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal)) return this.ValidationField("confirmNewPassword", "Password confirmation does not match.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Invite onboarding cannot be completed.");
        if (!IsInviteTokenValid(user, request.Token)) return this.ValidationForm("Invite onboarding cannot be completed.");
        if (user.InviteStatus != Skolio.Identity.Domain.Enums.IdentityInviteStatus.InviteConfirmed) return this.ValidationForm("Invite code confirmation is required.");
        if (user.InviteExpiresAtUtc is null || user.InviteExpiresAtUtc <= DateTimeOffset.UtcNow) return this.ValidationForm("Invite onboarding cannot be completed.");

        var confirmToken = DecodeToken(request.Token);
        var confirmResult = await userManager.ConfirmEmailAsync(user, confirmToken);
        if (!confirmResult.Succeeded && !user.EmailConfirmed) return BadRequest(ToValidationProblem(confirmResult));

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!resetResult.Succeeded) return BadRequest(ToValidationProblem(resetResult));

        user.AccountLifecycleStatus = Skolio.Identity.Domain.Enums.IdentityAccountLifecycleStatus.Active;
        user.InviteStatus = Skolio.Identity.Domain.Enums.IdentityInviteStatus.Active;
        user.ActivatedAtUtc = DateTimeOffset.UtcNow;
        user.OnboardingCompletedAtUtc = DateTimeOffset.UtcNow;
        user.InviteExpiresAtUtc = null;
        user.InviteTokenHash = null;
        await userManager.UpdateAsync(user);

        Audit("identity.security.invite.completed", user.Id, new { action = "invite-complete" });
        return Ok(new { message = "Account activation is complete." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var genericResponse = Ok(new { message = "If the account exists, a password reset email has been sent." });
        if (string.IsNullOrWhiteSpace(request.Email)) return genericResponse;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return genericResponse;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var resetUrl = BuildFrontendUrl($"/security/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}");

        await identityEmailSender.SendPasswordResetAsync(
            new PasswordResetEmailDelivery(
                user.Email ?? request.Email,
                BuildDisplayName(user),
                resetUrl,
                "token"),
            cancellationToken);

        Audit("identity.security.password.forgot-requested", user.Id, new { action = "forgot-password-request" });
        return genericResponse;
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal)) return this.ValidationField("confirmNewPassword", "Password confirmation does not match.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Reset token is invalid or expired.");

        var token = DecodeToken(request.Token);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        await userManager.UpdateSecurityStampAsync(user);
        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Password reset completed",
                "Your Skolio password reset was completed."),
            cancellationToken);

        Audit("identity.security.password.reset-completed", user.Id, new { action = "reset-password" });
        return Ok(new { message = "Password reset completed." });
    }

    [HttpPost("change-email/request")]
    [Authorize]
    [EnableRateLimiting("identity-security-change-email")]
    public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");
        if (string.IsNullOrWhiteSpace(request.NewEmail)) return this.ValidationField("newEmail", "New email is required.");

        var normalizedEmail = request.NewEmail.Trim();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, normalizedEmail);
        var encodedToken = EncodeToken(token);
        var verificationUrl = BuildFrontendUrl($"/security/confirm-email-change?userId={Uri.EscapeDataString(user.Id)}&newEmail={Uri.EscapeDataString(normalizedEmail)}&token={Uri.EscapeDataString(encodedToken)}");

        await identityEmailSender.SendChangeEmailVerificationAsync(
            new ChangeEmailVerificationDelivery(
                normalizedEmail,
                BuildDisplayName(user),
                verificationUrl,
                MaskEmail(normalizedEmail)),
            cancellationToken);

        Audit("identity.security.email-change.requested", user.Id, new { action = "change-email-requested", newEmailMasked = MaskEmail(normalizedEmail) });
        return Ok(new { message = "Email change verification was sent." });
    }

    [HttpPost("change-email/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-change-email")]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Email change token is invalid or expired.");

        var token = DecodeToken(request.Token);
        var oldEmail = user.Email ?? string.Empty;
        var result = await userManager.ChangeEmailAsync(user, request.NewEmail, token);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        user.UserName = request.NewEmail;
        var setUserNameResult = await userManager.UpdateAsync(user);
        if (!setUserNameResult.Succeeded) return BadRequest(ToValidationProblem(setUserNameResult));

        if (!string.IsNullOrWhiteSpace(oldEmail) && !string.Equals(oldEmail, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            await identityEmailSender.SendSecurityNotificationAsync(
                new SecurityNotificationDelivery(
                    oldEmail,
                    BuildDisplayName(user),
                    "Email changed",
                    $"Your Skolio sign-in email was changed to {MaskEmail(request.NewEmail)}."),
                cancellationToken);
        }

        Audit("identity.security.email-change.confirmed", user.Id, new { action = "change-email-confirmed", oldEmailMasked = MaskEmail(oldEmail), newEmailMasked = MaskEmail(request.NewEmail) });
        return Ok(new { message = "Email change confirmed." });
    }

    [HttpGet("mfa/status")]
    [Authorize]
    public async Task<ActionResult<MfaStatusContract>> MfaStatus(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var enabled = await userManager.GetTwoFactorEnabledAsync(user);
        var recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
        var hasAuthenticatorKey = !string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(user));

        return Ok(new MfaStatusContract(enabled, hasAuthenticatorKey, recoveryCodesLeft));
    }

    [HttpPost("mfa/setup/start")]
    [Authorize]
    public async Task<ActionResult<MfaSetupStartContract>> StartMfaSetup(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        await userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var sharedKey = FormatKey(unformattedKey);
        var issuer = "Skolio";
        var accountLabel = BuildMfaAccountLabel(user);
        var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString($"{issuer}:{accountLabel}")}?secret={Uri.EscapeDataString(unformattedKey)}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

        Audit("identity.security.mfa.setup-started", user.Id, new { action = "mfa-setup-started" });
        return Ok(new MfaSetupStartContract(sharedKey, authenticatorUri, issuer, accountLabel));
    }

    [HttpPost("mfa/setup/confirm")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<MfaSetupConfirmContract>> ConfirmMfaSetup([FromBody] ConfirmMfaSetupRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var verificationCode = NormalizeCode(request.VerificationCode);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
        if (!isValid) return this.ValidationField("verificationCode", "Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();

        await identityEmailSender.SendMfaChangedAsync(
            new MfaChangedDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                DateTimeOffset.UtcNow.ToString("O"),
                MaskIp(HttpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        Audit("identity.security.mfa.enabled", user.Id, new { action = "mfa-enabled" });
        return Ok(new MfaSetupConfirmContract(recoveryCodes));
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");

        var verificationCode = NormalizeCode(request.VerificationCode);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
        if (!isValid) return this.ValidationField("verificationCode", "Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        await identityEmailSender.SendMfaChangedAsync(
            new MfaChangedDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                DateTimeOffset.UtcNow.ToString("O"),
                MaskIp(HttpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        Audit("identity.security.mfa.disabled", user.Id, new { action = "mfa-disabled" });
        return Ok(new { message = "MFA disabled." });
    }

    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<RegenerateRecoveryCodesContract>> RegenerateRecoveryCodes([FromBody] RegenerateRecoveryCodesRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");
        if (!await userManager.GetTwoFactorEnabledAsync(user)) return this.ValidationForm("MFA is not enabled.");

        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();
        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Recovery codes regenerated",
                "Your Skolio recovery codes were regenerated."),
            cancellationToken);

        Audit("identity.security.mfa.recovery-codes-regenerated", user.Id, new { action = "mfa-recovery-codes-regenerated" });
        return Ok(new RegenerateRecoveryCodesContract(recoveryCodes));
    }

    private async Task<SkolioIdentityUser?> ResolveActorUser(CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(actor)) return null;
        return await userManager.FindByIdAsync(actor);
    }


    private static bool VerifyHash(string plain, string? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(expectedHash)) return false;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        var hashed = Convert.ToHexString(bytes);
        return string.Equals(hashed, expectedHash, StringComparison.Ordinal);
    }

    private static bool IsInviteTokenValid(SkolioIdentityUser user, string encodedToken)
    {
        if (string.IsNullOrWhiteSpace(encodedToken) || string.IsNullOrWhiteSpace(user.InviteTokenHash)) return false;
        return VerifyHash(encodedToken, user.InviteTokenHash);
    }

    private string BuildFrontendUrl(string pathAndQuery)
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return $"{origin.TrimEnd('/')}{pathAndQuery}";
        }

        return $"http://localhost:8080{pathAndQuery}";
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string DecodeToken(string token)
    {
        var bytes = WebEncoders.Base64UrlDecode(token);
        return Encoding.UTF8.GetString(bytes);
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

    private static string BuildDisplayName(SkolioIdentityUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email;
        return user.Id;
    }

    private static string BuildMfaAccountLabel(SkolioIdentityUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email;
        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
        return user.Id;
    }

    private static string MaskEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@')) return "masked";
        var parts = value.Split('@');
        var local = parts[0];
        if (local.Length <= 2) return $"**@{parts[1]}";
        return $"{local[0]}***{local[^1]}@{parts[1]}";
    }

    private static string MaskIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var segments = value.Split('.');
        if (segments.Length == 4) return $"{segments[0]}.{segments[1]}.*.*";
        return "masked";
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
            if (!details.Errors.ContainsKey(error.Code))
            {
                details.Errors[error.Code] = [error.Description];
            }
        }

        if (details.Errors.Count == 0)
        {
            details.Errors["$form"] = ["Validation failed."];
        }

        return details;
    }

    private void Audit(string actionCode, string targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "anonymous";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record SecuritySummaryContract(string UserId, string CurrentEmail, bool EmailConfirmed, bool MfaEnabled, bool HasAuthenticatorKey, int RecoveryCodesLeft);
    public sealed record InviteContextContract(string UserId, string EmailMasked, string InviteStatus, DateTimeOffset? ExpiresAtUtc);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string UserId, string Token, string NewPassword, string ConfirmNewPassword);
    public sealed record ResendActivationRequest(string Email);
    public sealed record ConfirmActivationRequest(string UserId, string Token);
    public sealed record ConfirmInviteCodeRequest(string UserId, string Token, string ActivationCode);
    public sealed record CompleteInviteRequest(string UserId, string Token, string NewPassword, string ConfirmNewPassword);
    public sealed record RequestEmailChangeRequest(string CurrentPassword, string NewEmail);
    public sealed record ConfirmEmailChangeRequest(string UserId, string NewEmail, string Token);
    public sealed record MfaStatusContract(bool Enabled, bool HasAuthenticatorKey, int RecoveryCodesLeft);
    public sealed record MfaSetupStartContract(string SharedKey, string AuthenticatorUri, string Issuer, string AccountLabel);
    public sealed record ConfirmMfaSetupRequest(string VerificationCode);
    public sealed record MfaSetupConfirmContract(IReadOnlyCollection<string> RecoveryCodes);
    public sealed record DisableMfaRequest(string CurrentPassword, string VerificationCode);
    public sealed record RegenerateRecoveryCodesRequest(string CurrentPassword);
    public sealed record RegenerateRecoveryCodesContract(IReadOnlyCollection<string> RecoveryCodes);
}

