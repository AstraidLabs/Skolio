using Microsoft.AspNetCore.Mvc;
using Skolio.EmailGateway.Api.Configuration;
using Skolio.EmailGateway.Api.Delivery;
using Skolio.EmailGateway.Api.Diagnostics;
using Skolio.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<EmailGatewayOptions>()
    .Bind(builder.Configuration.GetSection(EmailGatewayOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.AllowedTemplateTypes.Length > 0, "At least one template type must be allowed.")
    .ValidateOnStart();
builder.Services.AddSkolioServiceDefaults(builder.Configuration, "EmailGateway:Auth");
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IIdentityTemplateRenderer, IdentityTemplateRenderer>();
builder.Services.AddScoped<IEmailTransportSender, MailKitSmtpSender>();
builder.Services.AddHealthChecks().AddCheck<SmtpRelayHealthCheck>("smtp-relay", tags: ["ready"]);

var app = builder.Build();
var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailGatewayOptions>>().Value;
app.Logger.LogInformation("Starting {ServiceName} in {Environment}.", options.ServiceName, app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSkolioServiceDefaults(options.ServiceName);
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", () => Results.Ok(new { service = options.ServiceName, status = "phase-22-email-gateway-ready", mode = "identity-security-delivery-only", allowedTemplates = options.AllowedTemplateTypes }));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Stopping {ServiceName}.", options.ServiceName));
app.Run();
