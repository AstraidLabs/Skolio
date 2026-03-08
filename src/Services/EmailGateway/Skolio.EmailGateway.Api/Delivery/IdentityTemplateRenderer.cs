using Skolio.EmailGateway.Api.Contracts;

namespace Skolio.EmailGateway.Api.Delivery;

public sealed class IdentityTemplateRenderer : IIdentityTemplateRenderer
{
    public RenderedEmail RenderPasswordReset(PasswordResetEmailRequest request)
    {
        var subject = "Skolio: Password reset";
        var text = $"Hello {request.RecipientDisplayName},\n\nUse this secure link to reset your password:\n{request.ResetUrl}\n\nSecurity code reference: {request.SecurityCodeMasked}\n\nIf you did not request this change, contact your school administrator.";
        var html = $"<p>Hello {request.RecipientDisplayName},</p><p>Use this secure link to reset your password:</p><p><a href=\"{request.ResetUrl}\">Reset password</a></p><p>Security code reference: <strong>{request.SecurityCodeMasked}</strong></p><p>If you did not request this change, contact your school administrator.</p>";
        return new RenderedEmail(subject, text, html);
    }

    public RenderedEmail RenderChangeEmailVerification(ChangeEmailVerificationRequest request)
    {
        var subject = "Skolio: Change email verification";
        var text = $"Hello {request.RecipientDisplayName},\n\nConfirm your new email using this secure link:\n{request.VerificationUrl}\n\nNew email: {request.NewEmailMasked}";
        var html = $"<p>Hello {request.RecipientDisplayName},</p><p>Confirm your new email using this secure link:</p><p><a href=\"{request.VerificationUrl}\">Verify email change</a></p><p>New email: <strong>{request.NewEmailMasked}</strong></p>";
        return new RenderedEmail(subject, text, html);
    }

    public RenderedEmail RenderSecurityNotification(SecurityNotificationRequest request)
    {
        var subject = $"Skolio security notification: {request.NotificationTitle}";
        var text = $"Hello {request.RecipientDisplayName},\n\n{request.NotificationMessage}";
        var html = $"<p>Hello {request.RecipientDisplayName},</p><p>{request.NotificationMessage}</p>";
        return new RenderedEmail(subject, text, html);
    }

    public RenderedEmail RenderMfaChanged(MfaChangedRequest request)
    {
        var subject = "Skolio: MFA settings changed";
        var text = $"Hello {request.RecipientDisplayName},\n\nYour MFA settings were changed at {request.ChangedAtUtc}.\nSource IP: {request.ChangedByIpMasked}\n\nIf this was not you, contact your administrator immediately.";
        var html = $"<p>Hello {request.RecipientDisplayName},</p><p>Your MFA settings were changed at <strong>{request.ChangedAtUtc}</strong>.</p><p>Source IP: <strong>{request.ChangedByIpMasked}</strong></p><p>If this was not you, contact your administrator immediately.</p>";
        return new RenderedEmail(subject, text, html);
    }

    public RenderedEmail RenderAccountConfirmation(AccountConfirmationRequest request)
    {
        var subject = "Skolio: Account confirmation";
        var text = $"Hello {request.RecipientDisplayName},\n\nConfirm your account using this secure link:\n{request.ConfirmationUrl}";
        var html = $"<p>Hello {request.RecipientDisplayName},</p><p>Confirm your account using this secure link:</p><p><a href=\"{request.ConfirmationUrl}\">Confirm account</a></p>";
        return new RenderedEmail(subject, text, html);
    }
}
