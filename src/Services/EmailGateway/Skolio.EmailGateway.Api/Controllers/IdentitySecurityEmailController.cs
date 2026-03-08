using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skolio.EmailGateway.Api.Configuration;
using Skolio.EmailGateway.Api.Contracts;
using Skolio.EmailGateway.Api.Delivery;
using Skolio.EmailGateway.Api.Filters;

namespace Skolio.EmailGateway.Api.Controllers;

[ApiController]
[Route("internal/email-gateway")]
[ServiceFilter(typeof(InternalServiceAccessFilter))]
public sealed class IdentitySecurityEmailController(
    IOptions<EmailGatewayOptions> options,
    IIdentityTemplateRenderer templateRenderer,
    IEmailTransportSender transportSender,
    ILogger<IdentitySecurityEmailController> logger) : ControllerBase
{
    private readonly EmailGatewayOptions _options = options.Value;
    private readonly IIdentityTemplateRenderer _templateRenderer = templateRenderer;
    private readonly IEmailTransportSender _transportSender = transportSender;
    private readonly ILogger<IdentitySecurityEmailController> _logger = logger;

    [HttpPost("password-reset")]
    public Task<ActionResult<EmailDeliveryResponse>> DeliverPasswordReset([FromBody] PasswordResetEmailRequest request, CancellationToken cancellationToken)
        => DeliverAsync(IdentityEmailTemplateType.PasswordReset, request.RecipientEmail, request.RecipientDisplayName, _templateRenderer.RenderPasswordReset(request), cancellationToken);

    [HttpPost("change-email-verification")]
    public Task<ActionResult<EmailDeliveryResponse>> DeliverChangeEmailVerification([FromBody] ChangeEmailVerificationRequest request, CancellationToken cancellationToken)
        => DeliverAsync(IdentityEmailTemplateType.ChangeEmailVerification, request.RecipientEmail, request.RecipientDisplayName, _templateRenderer.RenderChangeEmailVerification(request), cancellationToken);

    [HttpPost("security-notification")]
    public Task<ActionResult<EmailDeliveryResponse>> DeliverSecurityNotification([FromBody] SecurityNotificationRequest request, CancellationToken cancellationToken)
        => DeliverAsync(IdentityEmailTemplateType.SecurityNotification, request.RecipientEmail, request.RecipientDisplayName, _templateRenderer.RenderSecurityNotification(request), cancellationToken);

    [HttpPost("mfa-changed")]
    public Task<ActionResult<EmailDeliveryResponse>> DeliverMfaChanged([FromBody] MfaChangedRequest request, CancellationToken cancellationToken)
        => DeliverAsync(IdentityEmailTemplateType.MfaChanged, request.RecipientEmail, request.RecipientDisplayName, _templateRenderer.RenderMfaChanged(request), cancellationToken);

    [HttpPost("account-confirmation")]
    public Task<ActionResult<EmailDeliveryResponse>> DeliverAccountConfirmation([FromBody] AccountConfirmationRequest request, CancellationToken cancellationToken)
        => DeliverAsync(IdentityEmailTemplateType.AccountConfirmation, request.RecipientEmail, request.RecipientDisplayName, _templateRenderer.RenderAccountConfirmation(request), cancellationToken);

    private async Task<ActionResult<EmailDeliveryResponse>> DeliverAsync(
        IdentityEmailTemplateType templateType,
        string recipientEmail,
        string recipientDisplayName,
        RenderedEmail renderedEmail,
        CancellationToken cancellationToken)
    {
        if (!_options.AllowedTemplateTypes.Contains(templateType.ToString(), StringComparer.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Template type not allowed.",
                Detail = $"Template type '{templateType}' is blocked by Email Gateway policy.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            await _transportSender.SendAsync(recipientEmail, recipientDisplayName, renderedEmail, cancellationToken);
            var correlationId = HttpContext.Response.Headers["X-Correlation-Id"].ToString();
            return Accepted(new EmailDeliveryResponse(true, templateType.ToString(), correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email delivery failed for template {TemplateType}.", templateType);
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Email relay unavailable.",
                Detail = "Email Gateway could not deliver the message via SMTP relay.",
                Status = StatusCodes.Status502BadGateway
            });
        }
    }
}
