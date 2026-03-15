using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.ServiceDefaults.Authorization;
using Skolio.Identity.Application.Abstractions;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/security-emails")]
public sealed class SecurityEmailsController(IIdentityEmailSender identityEmailSender) : ControllerBase
{
    [HttpPost("password-reset")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> SendPasswordReset([FromBody] PasswordResetEmailDelivery request, CancellationToken cancellationToken)
    {
        await identityEmailSender.SendPasswordResetAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("change-email-verification")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> SendChangeEmailVerification([FromBody] ChangeEmailVerificationDelivery request, CancellationToken cancellationToken)
    {
        await identityEmailSender.SendChangeEmailVerificationAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("security-notification")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> SendSecurityNotification([FromBody] SecurityNotificationDelivery request, CancellationToken cancellationToken)
    {
        await identityEmailSender.SendSecurityNotificationAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("mfa-changed")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> SendMfaChanged([FromBody] MfaChangedDelivery request, CancellationToken cancellationToken)
    {
        await identityEmailSender.SendMfaChangedAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("account-confirmation")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> SendAccountConfirmation([FromBody] AccountConfirmationDelivery request, CancellationToken cancellationToken)
    {
        await identityEmailSender.SendAccountConfirmationAsync(request, cancellationToken);
        return Accepted();
    }
}
