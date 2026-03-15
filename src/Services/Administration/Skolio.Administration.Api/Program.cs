using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Api.Auth;
using Skolio.Administration.Api.Configuration;
using Skolio.Administration.Api.Diagnostics;
using Skolio.Administration.Application;
using Skolio.Administration.Domain.Exceptions;
using Skolio.Administration.Infrastructure;
using Skolio.Administration.Infrastructure.Extensions;
using Skolio.Administration.Infrastructure.Persistence;
using Skolio.Administration.Infrastructure.Services;
using Skolio.ServiceDefaults.Authorization;
using Skolio.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AdministrationServiceOptions>().Bind(builder.Configuration.GetSection(AdministrationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddAdministrationApplication();
builder.Services.AddAdministrationInfrastructure(builder.Configuration);
builder.Services.AddSkolioServiceDefaults(builder.Configuration, "Administration:Auth");
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
builder.Services.AddOpenApi();
builder.Services.AddHangfireServer();
builder.Services.AddHealthChecks().AddDbContextCheck<AdministrationDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]).AddCheck<HangfireHealthCheck>("hangfire", tags: ["ready"]);

var app = builder.Build();
var enableLocalSeedMode = builder.Configuration.GetValue("Administration:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode) { await app.ApplyAdministrationMigrationsAsync(); }
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }

app.UseSkolioServiceDefaults("Skolio.Administration.Api", ex => ex is AdministrationDomainException);
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new HangfireDashboardAuthorizationFilter()] }).RequireAuthorization(SkolioPolicies.PlatformAdministration);
RecurringJob.AddOrUpdate<HousekeepingJob>("administration-housekeeping-boundary", job => job.ExecuteAsync(), Cron.HourInterval(6));
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AdministrationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl, hangfireDashboard = "/hangfire" }));
app.Run();
