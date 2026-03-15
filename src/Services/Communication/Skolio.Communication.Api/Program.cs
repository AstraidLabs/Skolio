using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Api.Configuration;
using Skolio.Communication.Api.Diagnostics;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application;
using Skolio.Communication.Domain.Exceptions;
using Skolio.Communication.Infrastructure;
using Skolio.Communication.Infrastructure.Extensions;
using Skolio.Communication.Infrastructure.Persistence;
using Skolio.ServiceDefaults.Authorization;
using Skolio.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<CommunicationServiceOptions>().Bind(builder.Configuration.GetSection(CommunicationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddCommunicationApplication();
builder.Services.AddCommunicationInfrastructure(builder.Configuration);
builder.Services.AddSkolioServiceDefaults(builder.Configuration, "Communication:Auth", configureCors: policy => policy.AllowCredentials());
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
builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("CommunicationApiWrite", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});
builder.Services.AddHealthChecks().AddDbContextCheck<CommunicationDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyCommunicationMigrationsAsync(); app.MapOpenApi(); }

app.UseSkolioServiceDefaults("Skolio.Communication.Api", ex => ex is CommunicationDomainException);
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("CommunicationApiWrite");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHub<CommunicationHub>("/hubs/communication").RequireAuthorization(SkolioPolicies.ParentStudentTeacherRead);
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<CommunicationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl, signalRHub = "/hubs/communication" }));
app.Run();
