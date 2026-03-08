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

public sealed record MfaChangedRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, MinLength(1)] string ChangedAtUtc,
    [property: Required, MinLength(1)] string ChangedByIpMasked);

public sealed record AccountConfirmationRequest(
    [property: Required, EmailAddress] string RecipientEmail,
    [property: Required, MinLength(1)] string RecipientDisplayName,
    [property: Required, Url] string ConfirmationUrl);

public sealed record EmailDeliveryResponse(bool Accepted, string TemplateType, string CorrelationId);
