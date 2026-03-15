using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Configuration;
using Skolio.Organization.Api.Diagnostics;
using Skolio.Organization.Application;
using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Infrastructure;
using Skolio.Organization.Infrastructure.Extensions;
using Skolio.Organization.Infrastructure.Persistence;
using Skolio.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OrganizationServiceOptions>()
    .Bind(builder.Configuration.GetSection(OrganizationServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOrganizationApplication();
builder.Services.AddOrganizationInfrastructure(builder.Configuration);
builder.Services.AddSkolioServiceDefaults(builder.Configuration, "Organization:Auth");
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };

        var correlationId = context.HttpContext.Response.Headers["X-Correlation-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            details.Extensions["correlationId"] = correlationId;
        }

        return new BadRequestObjectResult(details);
    };
});
builder.Services.AddRouting();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrganizationDbContext>(tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;

logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.Organization.Api", app.Environment.EnvironmentName);

var enableLocalSeedMode = builder.Configuration.GetValue("Organization:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode)
{
    await app.ApplyOrganizationMigrationsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSkolioServiceDefaults("Skolio.Organization.Api", ex => ex is OrganizationDomainException);
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<OrganizationServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "phase-7-operational-ready",
        publicBaseUrl = options.Value.PublicBaseUrl
    });
});

app.Lifetime.ApplicationStopping.Register(() => logger.LogInformation("Stopping {ServiceName}.", "Skolio.Organization.Api"));
app.Run();
