using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Skolio.EmailGateway.Api.Configuration;

namespace Skolio.EmailGateway.Api.Filters;

public sealed class InternalServiceAccessFilter(IOptions<EmailGatewayOptions> options, ILogger<InternalServiceAccessFilter> logger) : IAsyncActionFilter
{
    private readonly EmailGatewayOptions _options = options.Value;
    private readonly ILogger<InternalServiceAccessFilter> _logger = logger;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var headerValue = context.HttpContext.Request.Headers["X-Internal-Service-Key"].ToString();
        if (!string.Equals(headerValue, _options.InternalApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Internal access denied for {Path}.", context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Internal service access denied.",
                Status = StatusCodes.Status403Forbidden
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return Task.CompletedTask;
        }

        return next();
    }
}
