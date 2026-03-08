namespace Skolio.EmailGateway.Api.Delivery;

public sealed record RenderedEmail(string Subject, string TextBody, string HtmlBody);
