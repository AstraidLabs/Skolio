using System.ComponentModel.DataAnnotations;

namespace Skolio.EmailGateway.Api.Contracts;

public sealed record PasswordResetEmailRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, Url] string ResetUrl,
    [property: Required, MinLength(1)] string SecurityCodeMasked);

public sealed record ChangeEmailVerificationRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, Url] string VerificationUrl,
    [property: Required, MinLength(1)] string NewEmailMasked);

public sealed record SecurityNotificationRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, MinLength(1)] string NotificationTitle,
    [property: Required, MinLength(1)] string NotificationMessage);

public sealed class MfaChangedRequest
{
    [Required, EmailAddress]
    public string RecipientEmail { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string RecipientDisplayName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string ChangedAtUtc { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string ChangedByIpMasked { get; init; } = string.Empty;
}

public sealed record AccountConfirmationRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, Url] string ConfirmationUrl);

public sealed record EmailDeliveryResponse(bool Accepted, string TemplateType, string CorrelationId);

public sealed record AccountInviteRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, Url] string InviteUrl,
    [property: Required, MinLength(6)] string ActivationCode,
    [property: Required, MinLength(1)] string ExpiresAtUtc);
