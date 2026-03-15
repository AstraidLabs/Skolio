using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Skolio.ServiceDefaults.Middleware;

public static class CorrelationIdMiddlewareExtensions
{
    public static WebApplication UseSkolioCorrelationId(this WebApplication app, string serviceName)
    {
        var logger = app.Logger;
        var environmentName = app.Environment.EnvironmentName;

        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? Activity.Current?.TraceId.ToString()
                ?? context.TraceIdentifier;

            context.Response.Headers["X-Correlation-Id"] = correlationId;

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Service"] = serviceName,
                ["Environment"] = environmentName,
                ["CorrelationId"] = correlationId
            }))
            {
                await next();
            }
        });

        return app;
    }
}
