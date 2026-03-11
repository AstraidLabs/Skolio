namespace Skolio.Identity.Application.Abstractions;

public interface IIdentityEmailSender
{
    Task SendPasswordResetAsync(PasswordResetEmailDelivery delivery, CancellationToken cancellationToken);
    Task SendChangeEmailVerificationAsync(ChangeEmailVerificationDelivery delivery, CancellationToken cancellationToken);
    Task SendSecurityNotificationAsync(SecurityNotificationDelivery delivery, CancellationToken cancellationToken);
    Task SendMfaChangedAsync(MfaChangedDelivery delivery, CancellationToken cancellationToken);
    Task SendAccountConfirmationAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken);
    Task SendAccountInviteAsync(AccountInviteDelivery delivery, CancellationToken cancellationToken);
}

public sealed record PasswordResetEmailDelivery(string RecipientEmail, string RecipientDisplayName, string ResetUrl, string SecurityCodeMasked);
public sealed record ChangeEmailVerificationDelivery(string RecipientEmail, string RecipientDisplayName, string VerificationUrl, string NewEmailMasked);
public sealed record SecurityNotificationDelivery(string RecipientEmail, string RecipientDisplayName, string NotificationTitle, string NotificationMessage);
public sealed record MfaChangedDelivery(string RecipientEmail, string RecipientDisplayName, string ChangedAtUtc, string ChangedByIpMasked);
public sealed record AccountConfirmationDelivery(string RecipientEmail, string RecipientDisplayName, string ConfirmationUrl);

public sealed record AccountInviteDelivery(string RecipientEmail, string RecipientDisplayName, string InviteUrl, string ActivationCode, string ExpiresAtUtc);
