namespace Skolio.EmailGateway.Api.Delivery;

public interface IEmailTransportSender
{
    Task SendAsync(string recipientEmail, string recipientDisplayName, RenderedEmail email, CancellationToken cancellationToken);
}
