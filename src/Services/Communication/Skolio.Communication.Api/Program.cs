using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Api.Auth;
using Skolio.Communication.Api.Configuration;
using Skolio.Communication.Api.Diagnostics;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application;
using Skolio.Communication.Domain.Exceptions;
using Skolio.Communication.Infrastructure;
using Skolio.Communication.Infrastructure.Extensions;
using Skolio.Communication.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<CommunicationServiceOptions>().Bind(builder.Configuration.GetSection(CommunicationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<JwtValidationOptions>().Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? throw new InvalidOperationException("Missing Communication auth options.");
builder.Services.AddCommunicationApplication();builder.Services.AddCommunicationInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { options.Authority = jwtOptions.Authority; options.Audience = jwtOptions.Audience; options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SkolioPolicies.PlatformAdministration, policy => policy.RequireRole("PlatformAdministrator"));
    options.AddPolicy(SkolioPolicies.SharedAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.SchoolAdministrationOnly, policy => policy.RequireRole("SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministrationOnly, policy => policy.RequireRole("SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("SchoolAdministrator", "Teacher", "Parent", "Student"));
    options.AddPolicy(SkolioPolicies.StudentSelfService, policy => policy.RequireRole("Student"));
    options.AddPolicy(SkolioPolicies.PlatformAdminOverride, policy => policy.RequireRole("PlatformAdministrator"));
});
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddProblemDetails();builder.Services.AddSignalR();builder.Services.AddOpenApi();
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
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyCommunicationMigrationsAsync(); app.MapOpenApi(); }
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["Service"] = "Skolio.Communication.Api", ["Environment"] = app.Environment.EnvironmentName, ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = new ProblemDetails { Instance = context.Request.Path, Extensions = { ["correlationId"] = context.Response.Headers["X-Correlation-Id"].ToString() } };
    if (ex is CommunicationDomainException) { problem.Title = "Domain validation failed."; problem.Status = 400; problem.Detail = ex.Message; }
    else { problem.Title = "Unexpected server error."; problem.Status = 500; problem.Detail = "The request could not be completed."; }
    app.Logger.LogError(ex, "Unhandled exception for {Path}.", context.Request.Path);
    context.Response.StatusCode = problem.Status ?? 500;
    await context.Response.WriteAsJsonAsync(problem);
}));
app.UseCors("SkolioDevelopment");app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();app.MapControllers().RequireRateLimiting("CommunicationApiWrite");app.MapHealthChecks("/health/live");app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });app.MapHub<CommunicationHub>("/hubs/communication").RequireAuthorization(SkolioPolicies.ParentStudentTeacherRead);
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<CommunicationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl, signalRHub = "/hubs/communication" }));
app.Run();

