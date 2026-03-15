using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Configuration;
using Skolio.Academics.Api.Diagnostics;
using Skolio.Academics.Application;
using Skolio.Academics.Domain.Exceptions;
using Skolio.Academics.Infrastructure;
using Skolio.Academics.Infrastructure.Extensions;
using Skolio.Academics.Infrastructure.Persistence;
using Skolio.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AcademicsServiceOptions>().Bind(builder.Configuration.GetSection(AcademicsServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddAcademicsApplication();
builder.Services.AddAcademicsInfrastructure(builder.Configuration);
builder.Services.AddSkolioServiceDefaults(builder.Configuration, "Academics:Auth");
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
builder.Services.AddHealthChecks().AddDbContextCheck<AcademicsDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;
var enableLocalSeedMode = builder.Configuration.GetValue("Academics:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode) { await app.ApplyAcademicsMigrationsAsync(); }
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }

app.UseSkolioServiceDefaults("Skolio.Academics.Api", ex => ex is AcademicsDomainException);
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AcademicsServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl }));
app.Run();
