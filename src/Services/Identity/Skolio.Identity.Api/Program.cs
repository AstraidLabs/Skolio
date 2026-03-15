using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Api.Diagnostics;
using Skolio.Identity.Application;
using Skolio.Identity.Domain.Exceptions;
using Skolio.Identity.Infrastructure;
using Skolio.Identity.Infrastructure.Extensions;
using Skolio.Identity.Infrastructure.Persistence;
using Skolio.ServiceDefaults.Authorization;
using Skolio.ServiceDefaults.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<IdentityServiceOptions>().Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<BootstrapOptions>().Bind(builder.Configuration.GetSection(BootstrapOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("identity-security-forgot-password", limiterOptions =>
    {
        limiterOptions.PermitLimit = 8;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-security-reset-password", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-security-change-email", limiterOptions =>
    {
        limiterOptions.PermitLimit = 6;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-security-mfa-verify", limiterOptions =>
    {
        limiterOptions.PermitLimit = 12;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-security-invite-code", limiterOptions =>
    {
        limiterOptions.PermitLimit = 6;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-login-primary", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("identity-login-mfa-challenge", limiterOptions =>
    {
        limiterOptions.PermitLimit = 12;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
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
builder.Services.AddSkolioCors();
builder.Services.AddTransient<IClaimsTransformation, OpenIddictClaimsTransformation>();
builder.Services.AddSkolioAuthorization();

var app = builder.Build();
var logger = app.Logger;
logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.Identity.Api", app.Environment.EnvironmentName);
var enableLocalSeedMode = builder.Configuration.GetValue("Identity:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode) { await app.ApplyIdentityMigrationsAsync(); }
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }

app.UseSkolioCorrelationId("Skolio.Identity.Api");
app.UseSkolioExceptionHandler("Skolio.Identity.Api", ex => ex is IdentityDomainException);
app.UseCors(SkolioCorsExtensions.DevelopmentPolicyName);
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
