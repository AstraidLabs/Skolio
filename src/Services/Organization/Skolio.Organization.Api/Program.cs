using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Api.Configuration;
using Skolio.Organization.Api.Diagnostics;
using Skolio.Organization.Application;
using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Infrastructure;
using Skolio.Organization.Infrastructure.Configuration;
using Skolio.Organization.Infrastructure.Extensions;
using Skolio.Organization.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OrganizationServiceOptions>()
    .Bind(builder.Configuration.GetSection(OrganizationServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtValidationOptions>()
    .Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>()
    ?? throw new InvalidOperationException("Missing Organization auth options.");

builder.Services.AddOrganizationApplication();
builder.Services.AddOrganizationInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtOptions.Authority;
        options.Audience = jwtOptions.Audience;
        options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
    });

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

builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrganizationDbContext>(tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SkolioDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
var logger = app.Logger;

logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.Organization.Api", app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    await app.ApplyOrganizationMigrationsAsync();
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["Service"] = "Skolio.Organization.Api",
        ["Environment"] = app.Environment.EnvironmentName,
        ["CorrelationId"] = correlationId
    }))
    {
        await next();
    }
});

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;
        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();

        var problem = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        if (exception is OrganizationDomainException)
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
    });
});

app.UseCors("SkolioDevelopment");
app.UseAuthentication();
app.UseAuthorization();
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

