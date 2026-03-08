using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Skolio.EmailGateway.Api.Configuration;

namespace Skolio.EmailGateway.Api.Delivery;

public sealed class MailKitSmtpSender(IOptions<EmailGatewayOptions> options, ILogger<MailKitSmtpSender> logger) : IEmailTransportSender
{
    private readonly EmailGatewayOptions _options = options.Value;
    private readonly ILogger<MailKitSmtpSender> _logger = logger;

    public async Task SendAsync(string recipientEmail, string recipientDisplayName, RenderedEmail email, CancellationToken cancellationToken)
    {
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));
        message.To.Add(new MailboxAddress(recipientDisplayName, recipientEmail));
        message.Subject = email.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = email.TextBody,
            HtmlBody = email.HtmlBody
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        smtpClient.Timeout = _options.Smtp.CommandTimeoutSeconds * 1000;

        var secureSocketOptions = _options.Smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

        await smtpClient.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, secureSocketOptions, cancellationToken);

        if (_options.Smtp.RequireAuthentication)
        {
            await smtpClient.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, cancellationToken);
        }

        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email delivery succeeded to {RecipientDomain}.", MaskDomain(recipientEmail));
    }

    private static string MaskDomain(string email)
    {
        var at = email.IndexOf('@');
        return at < 0 ? "unknown" : email[(at + 1)..];
    }
}
