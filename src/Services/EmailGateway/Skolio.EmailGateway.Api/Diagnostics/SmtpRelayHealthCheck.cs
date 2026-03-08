using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Skolio.EmailGateway.Api.Configuration;

namespace Skolio.EmailGateway.Api.Diagnostics;

public sealed class SmtpRelayHealthCheck(IOptions<EmailGatewayOptions> options) : IHealthCheck
{
    private readonly EmailGatewayOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtpClient = new SmtpClient();
            smtpClient.Timeout = _options.Smtp.ConnectionTimeoutSeconds * 1000;
            var secureSocketOptions = _options.Smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

            await smtpClient.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, secureSocketOptions, cancellationToken);

            if (_options.Smtp.RequireAuthentication)
            {
                await smtpClient.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, cancellationToken);
            }

            await smtpClient.DisconnectAsync(true, cancellationToken);
            return HealthCheckResult.Healthy("SMTP relay reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("SMTP relay unreachable. Email delivery is currently limited.", ex);
        }
    }
}
