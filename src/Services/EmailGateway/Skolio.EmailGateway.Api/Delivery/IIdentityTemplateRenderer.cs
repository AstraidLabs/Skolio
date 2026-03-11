using Skolio.EmailGateway.Api.Contracts;

namespace Skolio.EmailGateway.Api.Delivery;

public interface IIdentityTemplateRenderer
{
    RenderedEmail RenderPasswordReset(PasswordResetEmailRequest request);
    RenderedEmail RenderChangeEmailVerification(ChangeEmailVerificationRequest request);
    RenderedEmail RenderSecurityNotification(SecurityNotificationRequest request);
    RenderedEmail RenderMfaChanged(MfaChangedRequest request);
    RenderedEmail RenderAccountConfirmation(AccountConfirmationRequest request);
    RenderedEmail RenderAccountInvite(AccountInviteRequest request);
}
