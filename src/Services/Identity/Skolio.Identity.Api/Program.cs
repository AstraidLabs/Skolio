using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Api.Diagnostics;
using Skolio.Identity.Application;
using Skolio.Identity.Domain.Exceptions;
using Skolio.Identity.Infrastructure;
using Skolio.Identity.Infrastructure.Extensions;
using Skolio.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<IdentityServiceOptions>().Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var isAuthPath = context.Request.Path.StartsWithSegments("/connect");
        return RateLimitPartition.GetFixedWindowLimiter($"{key}:{(isAuthPath ? "auth" : "default")}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = isAuthPath ? 20 : 120,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SkolioPolicies.PlatformAdministration, policy => policy.RequireRole("PlatformAdministrator"));
    options.AddPolicy(SkolioPolicies.SharedAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.SchoolAdministrationOnly, policy => policy.RequireRole("SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministrationOnly, policy => policy.RequireRole("SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("SchoolAdministrator", "Teacher", "Parent", "Student"));
    options.AddPolicy(SkolioPolicies.PlatformAdminOverride, policy => policy.RequireRole("PlatformAdministrator"));
});

var app = builder.Build();
var logger = app.Logger;
logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.Identity.Api", app.Environment.EnvironmentName);
if (app.Environment.IsDevelopment()) { await app.ApplyIdentityMigrationsAsync(); app.MapOpenApi(); }

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (logger.BeginScope(new Dictionary<string, object> { ["Service"] = "Skolio.Identity.Api", ["Environment"] = app.Environment.EnvironmentName, ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = feature?.Error;
    var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
    var problem = new ProblemDetails { Instance = context.Request.Path, Extensions = { ["correlationId"] = correlationId } };

    if (exception is IdentityDomainException)
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

app.UseCors("SkolioDevelopment");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<IdentityServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl, authWiring = "openiddict-authorization-server" }));
app.MapGet("/.well-known/jwks.json", () => Results.Redirect("/.well-known/openid-configuration/jwks"));
app.Lifetime.ApplicationStopping.Register(() => logger.LogInformation("Stopping {ServiceName}.", "Skolio.Identity.Api"));
app.Run();
