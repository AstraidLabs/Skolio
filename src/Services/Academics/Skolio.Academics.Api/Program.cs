using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Api.Configuration;
using Skolio.Academics.Api.Diagnostics;
using Skolio.Academics.Application;
using Skolio.Academics.Domain.Exceptions;
using Skolio.Academics.Infrastructure;
using Skolio.Academics.Infrastructure.Extensions;
using Skolio.Academics.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AcademicsServiceOptions>().Bind(builder.Configuration.GetSection(AcademicsServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<JwtValidationOptions>().Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? throw new InvalidOperationException("Missing Academics auth options.");
builder.Services.AddAcademicsApplication();
builder.Services.AddAcademicsInfrastructure(builder.Configuration);
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
builder.Services.AddRouting();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AcademicsDbContext>(tags: ["ready"]).AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
var logger = app.Logger;
if (app.Environment.IsDevelopment()) { await app.ApplyAcademicsMigrationsAsync(); app.MapOpenApi(); }

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (logger.BeginScope(new Dictionary<string, object> { ["Service"] = "Skolio.Academics.Api", ["Environment"] = app.Environment.EnvironmentName, ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = new ProblemDetails { Instance = context.Request.Path, Extensions = { ["correlationId"] = context.Response.Headers["X-Correlation-Id"].ToString() } };
    if (ex is AcademicsDomainException) { problem.Title = "Domain validation failed."; problem.Status = 400; problem.Detail = ex.Message; }
    else { problem.Title = "Unexpected server error."; problem.Status = 500; problem.Detail = "The request could not be completed."; }
    logger.LogError(ex, "Unhandled exception for {Path}.", context.Request.Path);
    context.Response.StatusCode = problem.Status ?? 500;
    await context.Response.WriteAsJsonAsync(problem);
}));

app.UseCors("SkolioDevelopment");app.UseAuthentication();app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AcademicsServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-7-operational-ready", publicBaseUrl = options.Value.PublicBaseUrl }));
app.Run();

