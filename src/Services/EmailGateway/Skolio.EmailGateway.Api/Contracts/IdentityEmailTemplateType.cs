namespace Skolio.EmailGateway.Api.Contracts;

public enum IdentityEmailTemplateType
{
    PasswordReset,
    ChangeEmailVerification,
    SecurityNotification,
    MfaChanged,
    AccountConfirmation
}
