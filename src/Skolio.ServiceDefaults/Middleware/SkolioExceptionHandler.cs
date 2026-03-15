using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Skolio.ServiceDefaults.Middleware;

public static class SkolioExceptionHandlerExtensions
{
    public static WebApplication UseSkolioExceptionHandler(
        this WebApplication app,
        string serviceName,
        Func<Exception, bool>? isDomainException = null)
    {
        var logger = app.Logger;

        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;
            var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();

            var problem = new ProblemDetails
            {
                Instance = context.Request.Path,
                Extensions = { ["correlationId"] = correlationId }
            };

            if (exception is not null && isDomainException?.Invoke(exception) == true)
            {
                problem.Title = "Domain validation failed.";
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Detail = exception.Message;
            }
            else
            {
                problem.Title = "Unexpected server error.";
                problem.Status = StatusCodes.Status500InternalServerError;
                problem.Detail = "The request could not be completed.";
            }

            logger.LogError(exception, "Unhandled exception for {Path}.", context.Request.Path);
            context.Response.StatusCode = problem.Status.Value;
            await context.Response.WriteAsJsonAsync(problem);
        }));

        return app;
    }
}
