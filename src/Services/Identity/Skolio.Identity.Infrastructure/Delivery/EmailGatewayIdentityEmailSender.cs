using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Infrastructure.Configuration;

namespace Skolio.Identity.Infrastructure.Delivery;

public sealed class EmailGatewayIdentityEmailSender(
    HttpClient httpClient,
    IOptions<EmailGatewayOptions> options,
    ILogger<EmailGatewayIdentityEmailSender> logger) : IIdentityEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient = httpClient;
    private readonly EmailGatewayOptions _options = options.Value;
    private readonly ILogger<EmailGatewayIdentityEmailSender> _logger = logger;

    public Task SendPasswordResetAsync(PasswordResetEmailDelivery delivery, CancellationToken cancellationToken) =>
        SendAsync("internal/email-gateway/password-reset", new
        {
            delivery.RecipientEmail,
            delivery.RecipientDisplayName,
            delivery.ResetUrl,
            delivery.SecurityCodeMasked
        }, cancellationToken);

    public Task SendChangeEmailVerificationAsync(ChangeEmailVerificationDelivery delivery, CancellationToken cancellationToken) =>
        SendAsync("internal/email-gateway/change-email-verification", new
        {
            delivery.RecipientEmail,
            delivery.RecipientDisplayName,
            delivery.VerificationUrl,
            delivery.NewEmailMasked
        }, cancellationToken);

    public Task SendSecurityNotificationAsync(SecurityNotificationDelivery delivery, CancellationToken cancellationToken) =>
        SendAsync("internal/email-gateway/security-notification", new
        {
            delivery.RecipientEmail,
            delivery.RecipientDisplayName,
            delivery.NotificationTitle,
            delivery.NotificationMessage
        }, cancellationToken);

    public Task SendMfaChangedAsync(MfaChangedDelivery delivery, CancellationToken cancellationToken) =>
        SendAsync("internal/email-gateway/mfa-changed", new
        {
            delivery.RecipientEmail,
            delivery.RecipientDisplayName,
            delivery.ChangedAtUtc,
            delivery.ChangedByIpMasked
        }, cancellationToken);

    public Task SendAccountConfirmationAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken) =>
        SendAsync("internal/email-gateway/account-confirmation", new
        {
            delivery.RecipientEmail,
            delivery.RecipientDisplayName,
            delivery.ConfirmationUrl
        }, cancellationToken);

    private async Task SendAsync(string relativeUrl, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        request.Headers.Add("X-Internal-Service-Key", _options.InternalApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Identity email delivery request accepted for {RelativeUrl}.", relativeUrl);
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Identity email delivery request failed for {RelativeUrl} with status {StatusCode}.", relativeUrl, (int)response.StatusCode);
        throw new InvalidOperationException($"Email Gateway rejected request with status {(int)response.StatusCode}: {body}");
    }
}
