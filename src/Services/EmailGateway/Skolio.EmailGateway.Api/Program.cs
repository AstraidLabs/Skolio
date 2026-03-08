using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Skolio.EmailGateway.Api.Configuration;
using Skolio.EmailGateway.Api.Delivery;
using Skolio.EmailGateway.Api.Diagnostics;
using Skolio.EmailGateway.Api.Filters;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<EmailGatewayOptions>()
    .Bind(builder.Configuration.GetSection(EmailGatewayOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => !string.IsNullOrWhiteSpace(options.InternalApiKey), "InternalApiKey is required.")
    .Validate(options => options.AllowedTemplateTypes.Length > 0, "At least one template type must be allowed.")
    .ValidateOnStart();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<InternalServiceAccessFilter>();
builder.Services.AddScoped<IIdentityTemplateRenderer, IdentityTemplateRenderer>();
builder.Services.AddScoped<IEmailTransportSender, MailKitSmtpSender>();
builder.Services.AddHealthChecks().AddCheck<SmtpRelayHealthCheck>("smtp-relay", tags: ["ready"]);

var app = builder.Build();
var logger = app.Logger;
var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailGatewayOptions>>().Value;
logger.LogInformation("Starting {ServiceName} in {Environment}.", options.ServiceName, app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (logger.BeginScope(new Dictionary<string, object> { ["Service"] = options.ServiceName, ["Environment"] = app.Environment.EnvironmentName, ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = feature?.Error;
    logger.LogError(exception, "Unhandled exception for {Path}.", context.Request.Path);

    var problem = new ProblemDetails
    {
        Title = "Unexpected server error.",
        Detail = "The request could not be completed.",
        Status = StatusCodes.Status500InternalServerError,
        Instance = context.Request.Path
    };

    context.Response.StatusCode = problem.Status.Value;
    await context.Response.WriteAsJsonAsync(problem);
}));

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", () => Results.Ok(new { service = options.ServiceName, status = "phase-22-email-gateway-ready", mode = "identity-security-delivery-only", allowedTemplates = options.AllowedTemplateTypes }));
app.Lifetime.ApplicationStopping.Register(() => logger.LogInformation("Stopping {ServiceName}.", options.ServiceName));
app.Run();
