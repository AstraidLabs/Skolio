using System.Diagnostics;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AdministrationServiceOptions>().Bind(builder.Configuration.GetSection(AdministrationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<JwtValidationOptions>().Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? throw new InvalidOperationException("Missing Administration auth options.");
builder.Services.AddAdministrationApplication();builder.Services.AddAdministrationInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { options.Authority = jwtOptions.Authority; options.Audience = jwtOptions.Audience; options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SkolioPolicies.PlatformAdministration, policy => policy.RequireRole("PlatformAdministrator"));
    options.AddPolicy(SkolioPolicies.SharedAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.SchoolAdministrationOnly, policy => policy.RequireRole("SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministrationOnly, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));
    options.AddPolicy(SkolioPolicies.StudentSelfService, policy => policy.RequireRole("Student"));
    options.AddPolicy(SkolioPolicies.PlatformAdminOverride, policy => policy.RequireRole("PlatformAdministrator"));
});
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
});builder.Services.AddRouting();builder.Services.AddProblemDetails();builder.Services.AddOpenApi();builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));builder.Services.AddHangfireServer();
builder.Services.AddHealthChecks().AddDbContextCheck<AdministrationDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]).AddCheck<HangfireHealthCheck>("hangfire", tags: ["ready"]);
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyAdministrationMigrationsAsync(); app.MapOpenApi(); }
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["Service"] = "Skolio.Administration.Api", ["Environment"] = app.Environment.EnvironmentName, ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = new ProblemDetails { Instance = context.Request.Path, Extensions = { ["correlationId"] = context.Response.Headers["X-Correlation-Id"].ToString() } };
    if (ex is AdministrationDomainException) { problem.Title = "Domain validation failed."; problem.Status = 400; problem.Detail = ex.Message; }
    else { problem.Title = "Unexpected server error."; problem.Status = 500; problem.Detail = "The request could not be completed."; }
    app.Logger.LogError(ex, "Unhandled exception for {Path}.", context.Request.Path);
    context.Response.StatusCode = problem.Status ?? 500;
    await context.Response.WriteAsJsonAsync(problem);
}));
app.UseCors("SkolioDevelopment");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.MapHealthChecks("/health/live");app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });app.MapHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new HangfireDashboardAuthorizationFilter()] }).RequireAuthorization(SkolioPolicies.PlatformAdministration);
RecurringJob.AddOrUpdate<HousekeepingJob>("administration-housekeeping-boundary", job => job.ExecuteAsync(), Cron.HourInterval(6));
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AdministrationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl, hangfireDashboard = "/hangfire" }));
app.Run();


