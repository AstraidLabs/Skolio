# Validation Phase Deliverable

## docs/governance/validation-model-frontend-backend.md

`$ext
# Validation Model Frontend/Backend

## Boundary
- Backend is the only source of truth for validation.
- Frontend performs only immediate UX validation.
- Frontend validation never replaces backend validation.
- Every request is validated again on backend even when frontend already validated form inputs.

## Frontend Validation Scope
- Required fields.
- Min/max length.
- Basic formats (email, phone, simple numeric/date ranges).
- Simple cross-field checks with immediate UX value.
- Submit disable when obvious invalid state exists.

## Backend Validation Scope
- Required/min/max/format validation.
- Domain invariants.
- School context, role/scope and ownership validation.
- Lifecycle and existence checks.
- Security-sensitive validation and authorization-related checks.

## Backend Source of Truth Placement
- Domain: invariants and domain rules.
- Application: command/query and use-case validation.
- API: receives request and returns standardized validation payload.
- Infrastructure: never authoritative validation source.

## Standard Validation Error Response
All service APIs return `ValidationProblemDetails` for validation failures.

- HTTP status: `400`
- `title`: `Validation failed.`
- `errors`: dictionary `fieldKey -> [messages]`
- Optional form-level key: `$form`

Example shape:

```json
{
  "title": "Validation failed.",
  "status": 400,
  "errors": {
    "email": ["Invalid email format."],
    "$form": ["Unable to save changes."]
  }
}
```

## Frontend Mapping Model
- Field-level errors are mapped from `errors[field]`.
- Form-level errors are mapped from `errors["$form"]` (and empty/global keys).
- Frontend keeps one shared mapping helper for backend validation payload.
- Frontend shows inline field errors and a form-level error area.

## Parity Rules
- Same required fields are required on frontend and backend.
- Same basic max length and format checks are mirrored on frontend.
- Complex business rules remain backend-only.
- Backend validation messages are mapped consistently into feature forms.

## Applied Services
- `Skolio.Identity.Api`
- `Skolio.Organization.Api`
- `Skolio.Academics.Api`
- `Skolio.Communication.Api`
- `Skolio.Administration.Api`
- `Skolio.Frontend`

## Forms Covered in This Phase
- My Profile
- Security (Change Password, Change Email)
- Parent self-service excuses
- Existing API-bound forms now consume standardized backend validation payload

## Out of Scope
This phase does not introduce:
- Generic platform-wide validation framework.
- Shared cross-service business validation library.
- Generic mega form engine.
- New business modules.
- Tests/quizzes/assessment/exams/automated grading.
- University model.
` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/ApiValidation.cs

`$ext
using Microsoft.AspNetCore.Mvc;

namespace Skolio.Academics.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
` 

## src/Services/Administration/Skolio.Administration.Api/Controllers/ApiValidation.cs

`$ext
using Microsoft.AspNetCore.Mvc;

namespace Skolio.Administration.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
` 

## src/Services/Communication/Skolio.Communication.Api/Controllers/ApiValidation.cs

`$ext
using Microsoft.AspNetCore.Mvc;

namespace Skolio.Communication.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
` 

## src/Services/Identity/Skolio.Identity.Api/Controllers/ApiValidation.cs

`$ext
using Microsoft.AspNetCore.Mvc;

namespace Skolio.Identity.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/ApiValidation.cs

`$ext
using Microsoft.AspNetCore.Mvc;

namespace Skolio.Organization.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
` 

## src/Services/Academics/Skolio.Academics.Api/Program.cs

`$ext
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


` 

## src/Services/Administration/Skolio.Administration.Api/Program.cs

`$ext
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


` 

## src/Services/Communication/Skolio.Communication.Api/Program.cs

`$ext
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
});builder.Services.AddRouting();builder.Services.AddProblemDetails();builder.Services.AddSignalR();builder.Services.AddOpenApi();
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


` 

## src/Services/Identity/Skolio.Identity.Api/Program.cs

`$ext
using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
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

builder.Services.AddTransient<IClaimsTransformation, OpenIddictClaimsTransformation>();
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

var app = builder.Build();
var logger = app.Logger;
logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.Identity.Api", app.Environment.EnvironmentName);
var enableLocalSeedMode = builder.Configuration.GetValue("Identity:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode) { await app.ApplyIdentityMigrationsAsync(); }
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }

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


` 

## src/Services/Organization/Skolio.Organization.Api/Program.cs

`$ext
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
});
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

var enableLocalSeedMode = builder.Configuration.GetValue("Organization:Seed:EnableLocalMode", false);
if (app.Environment.IsDevelopment() || enableLocalSeedMode)
{
    await app.ApplyOrganizationMigrationsAsync();
}

if (app.Environment.IsDevelopment())
{
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


` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/AttendanceController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Attendance;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Excuses;
using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/attendance")]
public sealed class AttendanceController(IMediator mediator, AcademicsDbContext dbContext, ILogger<AttendanceController> logger) : ControllerBase
{
    private static readonly TimeSpan ParentExcuseUpdateWindow = TimeSpan.FromHours(48);

    [HttpGet("records")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordContract>>> Records([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.AttendanceRecords.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (studentUserId.HasValue)
        {
            query = query.Where(x => x.StudentUserId == studentUserId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();
            query = query.Where(x => x.StudentUserId == actorUserId);
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return this.ValidationField("audienceId", "Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceRecordContract(x.Id, x.SchoolId, x.AudienceId, x.StudentUserId, x.AttendanceDate, x.Status))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("records")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AttendanceRecordContract>> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordAttendanceCommand(request.SchoolId, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status), cancellationToken);
        Audit("academics.attendance.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status });
        return CreatedAtAction(nameof(RecordAttendance), new { id = result.Id }, result);
    }

    [HttpPut("records/{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<AttendanceRecordContract>> OverrideAttendance(Guid id, [FromBody] OverrideAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.attendance.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Status });
        return Ok(new AttendanceRecordContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.StudentUserId, entity.AttendanceDate, entity.Status));
    }

    [HttpGet("excuse-notes")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ExcuseNoteContract>>> ExcuseNotes([FromQuery] Guid schoolId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.ExcuseNotes
            .Join(dbContext.AttendanceRecords, excuse => excuse.AttendanceRecordId, attendance => attendance.Id, (excuse, attendance) => new { excuse, attendance })
            .Where(x => x.attendance.SchoolId == schoolId);

        if (studentUserId.HasValue)
        {
            query = query.Where(x => x.attendance.StudentUserId == studentUserId.Value);
        }

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.excuse.ParentUserId == actorUserId);

            if (studentUserId.HasValue && !SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();
            query = query.Where(x => x.attendance.StudentUserId == actorUserId);
        }

        var result = await query
            .OrderByDescending(x => x.excuse.SubmittedAtUtc)
            .Select(x => new ExcuseNoteContract(x.excuse.Id, x.excuse.AttendanceRecordId, x.excuse.ParentUserId, x.excuse.Reason, x.excuse.SubmittedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("my/excuse-requests")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ExcuseNoteContract>>> MyExcuseRequests(CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var linkedStudentIds = SchoolScope.GetLinkedStudentIds(User);
        if (linkedStudentIds.Count == 0) return Ok(Array.Empty<ExcuseNoteContract>());

        var result = await dbContext.ExcuseNotes
            .Join(dbContext.AttendanceRecords, excuse => excuse.AttendanceRecordId, attendance => attendance.Id, (excuse, attendance) => new { excuse, attendance })
            .Where(x => x.excuse.ParentUserId == actorUserId && linkedStudentIds.Contains(x.attendance.StudentUserId))
            .OrderByDescending(x => x.excuse.SubmittedAtUtc)
            .Select(x => new ExcuseNoteContract(x.excuse.Id, x.excuse.AttendanceRecordId, x.excuse.ParentUserId, x.excuse.Reason, x.excuse.SubmittedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("my/excuse-requests")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitMyExcuse([FromBody] SubmitMyExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();

        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, actorUserId, request.Reason), cancellationToken);
        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "create", result.Id, request.AttendanceRecordId, parentUserId = actorUserId, selfService = true });
        return CreatedAtAction(nameof(SubmitMyExcuse), new { id = result.Id }, result);
    }

    [HttpPut("my/excuse-requests/{id:guid}")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> UpdateMyExcuse(Guid id, [FromBody] UpdateExcuseRequest request, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.ParentUserId != actorUserId) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse update window expired.");

        entity.UpdateByParent(request.Reason, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "update", entity.Id, parentUserId = actorUserId, selfService = true });
        return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
    }

    [HttpDelete("my/excuse-requests/{id:guid}")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> CancelMyExcuse(Guid id, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.ParentUserId != actorUserId) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse cancellation window expired.");

        dbContext.ExcuseNotes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "cancel", entity.Id, parentUserId = actorUserId, selfService = true });
        return NoContent();
    }

    [HttpPost("excuse-notes")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitExcuse([FromBody] SubmitExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || request.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == attendance.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == attendance.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, request.ParentUserId, request.Reason), cancellationToken);
        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "create", result.Id, request.AttendanceRecordId, request.ParentUserId });
        return CreatedAtAction(nameof(SubmitExcuse), new { id = result.Id }, result);
    }

    [HttpPut("excuse-notes/{id:guid}")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> UpdateExcuse(Guid id, [FromBody] UpdateExcuseRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || entity.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
            if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse update window expired.");

            entity.UpdateByParent(request.Reason, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "update", entity.Id });
            return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }

        return Forbid();
    }

    [HttpDelete("excuse-notes/{id:guid}")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> CancelExcuse(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || entity.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
            if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse cancellation window expired.");

            dbContext.ExcuseNotes.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "cancel", entity.Id });
            return NoContent();
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }

        return Forbid();
    }

    [HttpPut("excuse-notes/{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ExcuseNoteContract>> OverrideExcuse(Guid id, [FromBody] OverrideExcuseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Reason, request.SubmittedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        Audit("academics.excuse-note.override", attendance?.SchoolId ?? Guid.Empty, new { request.OverrideReason, entity.Id });
        return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordAttendanceRequest(Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status);
    public sealed record OverrideAttendanceRequest(Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status, string OverrideReason);
    public sealed record SubmitExcuseNoteRequest(Guid AttendanceRecordId, Guid ParentUserId, string Reason);
    public sealed record SubmitMyExcuseNoteRequest(Guid AttendanceRecordId, string Reason);
    public sealed record UpdateExcuseRequest(string Reason);
    public sealed record OverrideExcuseRequest(string Reason, DateTimeOffset SubmittedAtUtc, string OverrideReason);
}

` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/DailyReportsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.DailyReports;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/daily-reports")]
public sealed class DailyReportsController(IMediator mediator, AcademicsDbContext dbContext, ILogger<DailyReportsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<DailyReportContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.DailyReports.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return this.ValidationField("audienceId", "Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderByDescending(x => x.ReportDate).Select(x => new DailyReportContract(x.Id, x.SchoolId, x.AudienceId, x.ReportDate, x.Summary, x.Notes)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<DailyReportContract>> Record([FromBody] RecordDailyReportRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordDailyReportCommand(request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes), cancellationToken);
        Audit("academics.daily-report.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.ReportDate });
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<DailyReportContract>> OverrideDailyReport(Guid id, [FromBody] OverrideDailyReportRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.DailyReports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.ReportDate, request.Summary, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.daily-report.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.ReportDate });
        return Ok(new DailyReportContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.ReportDate, entity.Summary, entity.Notes));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordDailyReportRequest(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
    public sealed record OverrideDailyReportRequest(Guid AudienceId, DateOnly ReportDate, string Summary, string Notes, string OverrideReason);
}

` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/GradesController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Grades;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/grades")]
public sealed class GradesController(IMediator mediator, AcademicsDbContext dbContext, ILogger<GradesController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<GradeEntryContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid studentUserId, [FromQuery] Guid subjectId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        if (IsParentOnly())
        {
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || studentUserId != actorUserId) return Forbid();
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.SubjectId == subjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await dbContext.GradeEntries
            .Where(x => x.StudentUserId == studentUserId && x.SubjectId == subjectId)
            .OrderByDescending(x => x.GradedOn)
            .Select(x => new GradeEntryContract(x.Id, x.StudentUserId, x.SubjectId, x.GradeValue, x.Note, x.GradedOn))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<GradeEntryContract>> RecordGrade([FromBody] RecordGradeRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.SubjectId == request.SubjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordGradeEntryCommand(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn), cancellationToken);
        Audit("academics.grade.changed", request.SchoolId, new { operation = "create", result.Id, request.StudentUserId, request.SubjectId, request.GradeValue });
        return CreatedAtAction(nameof(RecordGrade), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<GradeEntryContract>> OverrideGrade(Guid id, [FromBody] OverrideGradeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.GradeEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.grade.override", request.SchoolId, new { request.OverrideReason, entity.Id, request.GradeValue });
        return Ok(new GradeEntryContract(entity.Id, entity.StudentUserId, entity.SubjectId, entity.GradeValue, entity.Note, entity.GradedOn));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
    public sealed record OverrideGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn, string OverrideReason);
}

` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/HomeworkController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Homework;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/homework")]
public sealed class HomeworkController(IMediator mediator, AcademicsDbContext dbContext, ILogger<HomeworkController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<HomeworkAssignmentContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.HomeworkAssignments.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return this.ValidationField("audienceId", "Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderBy(x => x.DueDate).Select(x => new HomeworkAssignmentContract(x.Id, x.SchoolId, x.AudienceId, x.SubjectId, x.Title, x.Instructions, x.DueDate)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<HomeworkAssignmentContract>> Assign([FromBody] AssignHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId && x.SubjectId == request.SubjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new AssignHomeworkCommand(request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate), cancellationToken);
        Audit("academics.homework.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.SubjectId, request.DueDate });
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<HomeworkAssignmentContract>> OverrideHomework(Guid id, [FromBody] OverrideHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.HomeworkAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.homework.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Title });
        return Ok(new HomeworkAssignmentContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.SubjectId, entity.Title, entity.Instructions, entity.DueDate));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record AssignHomeworkRequest(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
    public sealed record OverrideHomeworkRequest(Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate, string OverrideReason);
}

` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/LessonsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Lessons;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/lessons")]
public sealed class LessonsController(IMediator mediator, AcademicsDbContext dbContext, ILogger<LessonsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<LessonRecordContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? timetableEntryId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.LessonRecords.Join(dbContext.TimetableEntries, lesson => lesson.TimetableEntryId, timetable => timetable.Id, (lesson, timetable) => new { lesson, timetable })
            .Where(x => x.timetable.SchoolId == schoolId);

        if (timetableEntryId.HasValue)
        {
            query = query.Where(x => x.timetable.Id == timetableEntryId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.timetable.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.timetable.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.timetable.TeacherUserId == actorUserId);
        }

        return Ok(await query.OrderByDescending(x => x.lesson.LessonDate)
            .Select(x => new LessonRecordContract(x.lesson.Id, x.lesson.TimetableEntryId, x.lesson.LessonDate, x.lesson.Topic, x.lesson.Summary))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<LessonRecordContract>> Record([FromBody] RecordLessonRequest request, CancellationToken cancellationToken)
    {
        var timetable = await dbContext.TimetableEntries.FirstOrDefaultAsync(x => x.Id == request.TimetableEntryId, cancellationToken);
        if (timetable is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, timetable.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || timetable.TeacherUserId != actorUserId) return Forbid();
        }

        var result = await mediator.Send(new RecordLessonCommand(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary), cancellationToken);
        Audit("academics.lesson-record.changed", timetable.SchoolId, new { operation = "create", result.Id, request.TimetableEntryId, request.LessonDate, request.Topic });
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<LessonRecordContract>> OverrideLesson(Guid id, [FromBody] OverrideLessonRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.LessonRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary);
        await dbContext.SaveChangesAsync(cancellationToken);

        var schoolId = await dbContext.TimetableEntries.Where(x => x.Id == request.TimetableEntryId).Select(x => x.SchoolId).FirstOrDefaultAsync(cancellationToken);
        Audit("academics.lesson.override", schoolId, new { request.OverrideReason, entity.Id, request.Topic });
        return Ok(new LessonRecordContract(entity.Id, entity.TimetableEntryId, entity.LessonDate, entity.Topic, entity.Summary));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
    public sealed record OverrideLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary, string OverrideReason);
}

` 

## src/Services/Academics/Skolio.Academics.Api/Controllers/TimetableController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Timetable;
using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/timetable")]
public sealed class TimetableController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<TimetableEntryContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TimetableEntries.Where(x => x.SchoolId == schoolId);

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.TeacherUserId == actorUserId);
        }

        return Ok(await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(x => new TimetableEntryContract(x.Id, x.SchoolId, x.SchoolYearId, x.DayOfWeek, x.StartTime, x.EndTime, x.AudienceType, x.AudienceId, x.SubjectId, x.TeacherUserId)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<TimetableEntryContract>> Create([FromBody] CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new CreateTimetableEntryCommand(request.SchoolId, request.SchoolYearId, request.DayOfWeek, request.StartTime, request.EndTime, request.AudienceType, request.AudienceId, request.SubjectId, request.TeacherUserId), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public sealed record CreateTimetableEntryRequest(Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId);
}

` 

## src/Services/Communication/Skolio.Communication.Api/Controllers/AnnouncementsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Api.Auth;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application.Announcements;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Api.Controllers;

[ApiController]
[Route("api/communication/announcements")]
public sealed class AnnouncementsController(IMediator mediator, IHubContext<CommunicationHub> hubContext, CommunicationDbContext dbContext, ILogger<AnnouncementsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<AnnouncementContract>>> List([FromQuery] Guid schoolId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.Announcements.Where(x => x.SchoolId == schoolId);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        return Ok(await query.OrderByDescending(x => x.PublishAtUtc)
            .Select(x => new AnnouncementContract(x.Id, x.SchoolId, x.Title, x.Message, x.PublishAtUtc, x.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<AnnouncementContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AnnouncementContract>> Publish([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        Audit("communication.school-announcement.published", result.Id, new { request.SchoolId, request.Title });
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPost("platform")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<AnnouncementContract>> PublishPlatformAnnouncement([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        Audit("communication.platform-announcement.published", result.Id, new { request.SchoolId, request.Title });
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<AnnouncementContract>> OverrideAnnouncement(Guid id, [FromBody] OverrideAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Title, request.Message, request.PublishAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("communication.announcement.override", id, new { request.OverrideReason, request.Title });
        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    [HttpPut("{id:guid}/deactivation")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AnnouncementContract>> SetActivation(Guid id, [FromBody] SetAnnouncementActivationRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "communication.announcement.activated" : "communication.announcement.deactivated", id, new { request.IsActive });
        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record PublishAnnouncementRequest(Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc);
    public sealed record OverrideAnnouncementRequest(string Title, string Message, DateTimeOffset PublishAtUtc, string OverrideReason);
    public sealed record SetAnnouncementActivationRequest(bool IsActive);
}




` 

## src/Services/Identity/Skolio.Identity.Api/Controllers/IdentitySecurityController.cs

`$ext
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/security")]
public sealed class IdentitySecurityController(
    UserManager<SkolioIdentityUser> userManager,
    SignInManager<SkolioIdentityUser> signInManager,
    IIdentityEmailSender identityEmailSender,
    ILogger<IdentitySecurityController> logger) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize]
    public async Task<ActionResult<SecuritySummaryContract>> Summary(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
        var hasAuthenticatorKey = !string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(user));

        return Ok(new SecuritySummaryContract(
            user.Id,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            await userManager.GetTwoFactorEnabledAsync(user),
            hasAuthenticatorKey,
            recoveryCodesLeft));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal)) return this.ValidationField("confirmNewPassword", "Password confirmation does not match.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.RefreshSignInAsync(user);

        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Password changed",
                "Your Skolio password was changed successfully."),
            cancellationToken);

        Audit("identity.security.password.changed", user.Id, new { action = "change-password" });
        return Ok(new { message = "Password changed." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var genericResponse = Ok(new { message = "If the account exists, a password reset email has been sent." });
        if (string.IsNullOrWhiteSpace(request.Email)) return genericResponse;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return genericResponse;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var resetUrl = BuildFrontendUrl($"/security/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}");

        await identityEmailSender.SendPasswordResetAsync(
            new PasswordResetEmailDelivery(
                user.Email ?? request.Email,
                BuildDisplayName(user),
                resetUrl,
                "token"),
            cancellationToken);

        Audit("identity.security.password.forgot-requested", user.Id, new { action = "forgot-password-request" });
        return genericResponse;
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal)) return this.ValidationField("confirmNewPassword", "Password confirmation does not match.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Reset token is invalid or expired.");

        var token = DecodeToken(request.Token);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        await userManager.UpdateSecurityStampAsync(user);
        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Password reset completed",
                "Your Skolio password reset was completed."),
            cancellationToken);

        Audit("identity.security.password.reset-completed", user.Id, new { action = "reset-password" });
        return Ok(new { message = "Password reset completed." });
    }

    [HttpPost("change-email/request")]
    [Authorize]
    [EnableRateLimiting("identity-security-change-email")]
    public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");
        if (string.IsNullOrWhiteSpace(request.NewEmail)) return this.ValidationField("newEmail", "New email is required.");

        var normalizedEmail = request.NewEmail.Trim();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, normalizedEmail);
        var encodedToken = EncodeToken(token);
        var verificationUrl = BuildFrontendUrl($"/security/confirm-email-change?userId={Uri.EscapeDataString(user.Id)}&newEmail={Uri.EscapeDataString(normalizedEmail)}&token={Uri.EscapeDataString(encodedToken)}");

        await identityEmailSender.SendChangeEmailVerificationAsync(
            new ChangeEmailVerificationDelivery(
                normalizedEmail,
                BuildDisplayName(user),
                verificationUrl,
                MaskEmail(normalizedEmail)),
            cancellationToken);

        Audit("identity.security.email-change.requested", user.Id, new { action = "change-email-requested", newEmailMasked = MaskEmail(normalizedEmail) });
        return Ok(new { message = "Email change verification was sent." });
    }

    [HttpPost("change-email/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-security-change-email")]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return this.ValidationForm("Email change token is invalid or expired.");

        var token = DecodeToken(request.Token);
        var oldEmail = user.Email ?? string.Empty;
        var result = await userManager.ChangeEmailAsync(user, request.NewEmail, token);
        if (!result.Succeeded) return BadRequest(ToValidationProblem(result));

        user.UserName = request.NewEmail;
        var setUserNameResult = await userManager.UpdateAsync(user);
        if (!setUserNameResult.Succeeded) return BadRequest(ToValidationProblem(setUserNameResult));

        if (!string.IsNullOrWhiteSpace(oldEmail) && !string.Equals(oldEmail, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            await identityEmailSender.SendSecurityNotificationAsync(
                new SecurityNotificationDelivery(
                    oldEmail,
                    BuildDisplayName(user),
                    "Email changed",
                    $"Your Skolio sign-in email was changed to {MaskEmail(request.NewEmail)}."),
                cancellationToken);
        }

        Audit("identity.security.email-change.confirmed", user.Id, new { action = "change-email-confirmed", oldEmailMasked = MaskEmail(oldEmail), newEmailMasked = MaskEmail(request.NewEmail) });
        return Ok(new { message = "Email change confirmed." });
    }

    [HttpGet("mfa/status")]
    [Authorize]
    public async Task<ActionResult<MfaStatusContract>> MfaStatus(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var enabled = await userManager.GetTwoFactorEnabledAsync(user);
        var recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
        var hasAuthenticatorKey = !string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(user));

        return Ok(new MfaStatusContract(enabled, hasAuthenticatorKey, recoveryCodesLeft));
    }

    [HttpPost("mfa/setup/start")]
    [Authorize]
    public async Task<ActionResult<MfaSetupStartContract>> StartMfaSetup(CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        await userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var sharedKey = FormatKey(unformattedKey);
        var authenticatorUri = $"otpauth://totp/Skolio:{Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id)}?secret={Uri.EscapeDataString(unformattedKey)}&issuer=Skolio&digits=6";

        Audit("identity.security.mfa.setup-started", user.Id, new { action = "mfa-setup-started" });
        return Ok(new MfaSetupStartContract(sharedKey, authenticatorUri));
    }

    [HttpPost("mfa/setup/confirm")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<MfaSetupConfirmContract>> ConfirmMfaSetup([FromBody] ConfirmMfaSetupRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var verificationCode = NormalizeCode(request.VerificationCode);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
        if (!isValid) return this.ValidationField("verificationCode", "Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();

        await identityEmailSender.SendMfaChangedAsync(
            new MfaChangedDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                DateTimeOffset.UtcNow.ToString("O"),
                MaskIp(HttpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        Audit("identity.security.mfa.enabled", user.Id, new { action = "mfa-enabled" });
        return Ok(new MfaSetupConfirmContract(recoveryCodes));
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");

        var verificationCode = NormalizeCode(request.VerificationCode);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
        if (!isValid) return this.ValidationField("verificationCode", "Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        await identityEmailSender.SendMfaChangedAsync(
            new MfaChangedDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                DateTimeOffset.UtcNow.ToString("O"),
                MaskIp(HttpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        Audit("identity.security.mfa.disabled", user.Id, new { action = "mfa-disabled" });
        return Ok(new { message = "MFA disabled." });
    }

    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    [EnableRateLimiting("identity-security-mfa-verify")]
    public async Task<ActionResult<RegenerateRecoveryCodesContract>> RegenerateRecoveryCodes([FromBody] RegenerateRecoveryCodesRequest request, CancellationToken cancellationToken)
    {
        var user = await ResolveActorUser(cancellationToken);
        if (user is null) return Forbid();

        var reauth = await signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: false);
        if (!reauth.Succeeded) return this.ValidationField("currentPassword", "Current password is invalid.");
        if (!await userManager.GetTwoFactorEnabledAsync(user)) return this.ValidationForm("MFA is not enabled.");

        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();
        await identityEmailSender.SendSecurityNotificationAsync(
            new SecurityNotificationDelivery(
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                "Recovery codes regenerated",
                "Your Skolio recovery codes were regenerated."),
            cancellationToken);

        Audit("identity.security.mfa.recovery-codes-regenerated", user.Id, new { action = "mfa-recovery-codes-regenerated" });
        return Ok(new RegenerateRecoveryCodesContract(recoveryCodes));
    }

    private async Task<SkolioIdentityUser?> ResolveActorUser(CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(actor)) return null;
        return await userManager.FindByIdAsync(actor);
    }

    private string BuildFrontendUrl(string pathAndQuery)
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return $"{origin.TrimEnd('/')}{pathAndQuery}";
        }

        return $"http://localhost:8080{pathAndQuery}";
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string DecodeToken(string token)
    {
        var bytes = WebEncoders.Base64UrlDecode(token);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private static string FormatKey(string unformattedKey)
    {
        if (string.IsNullOrWhiteSpace(unformattedKey)) return string.Empty;
        var builder = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i++)
        {
            if (i > 0 && i % 4 == 0) builder.Append(' ');
            builder.Append(char.ToLowerInvariant(unformattedKey[i]));
        }

        return builder.ToString();
    }

    private static string BuildDisplayName(SkolioIdentityUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email;
        return user.Id;
    }

    private static string MaskEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@')) return "masked";
        var parts = value.Split('@');
        var local = parts[0];
        if (local.Length <= 2) return $"**@{parts[1]}";
        return $"{local[0]}***{local[^1]}@{parts[1]}";
    }

    private static string MaskIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var segments = value.Split('.');
        if (segments.Length == 4) return $"{segments[0]}.{segments[1]}.*.*";
        return "masked";
    }

    private static ValidationProblemDetails ToValidationProblem(IdentityResult result)
    {
        var details = new ValidationProblemDetails
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        };
        foreach (var error in result.Errors)
        {
            if (!details.Errors.ContainsKey(error.Code))
            {
                details.Errors[error.Code] = [error.Description];
            }
        }

        if (details.Errors.Count == 0)
        {
            details.Errors["$form"] = ["Validation failed."];
        }

        return details;
    }

    private void Audit(string actionCode, string targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "anonymous";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record SecuritySummaryContract(string UserId, string CurrentEmail, bool EmailConfirmed, bool MfaEnabled, bool HasAuthenticatorKey, int RecoveryCodesLeft);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string UserId, string Token, string NewPassword, string ConfirmNewPassword);
    public sealed record RequestEmailChangeRequest(string CurrentPassword, string NewEmail);
    public sealed record ConfirmEmailChangeRequest(string UserId, string NewEmail, string Token);
    public sealed record MfaStatusContract(bool Enabled, bool HasAuthenticatorKey, int RecoveryCodesLeft);
    public sealed record MfaSetupStartContract(string SharedKey, string AuthenticatorUri);
    public sealed record ConfirmMfaSetupRequest(string VerificationCode);
    public sealed record MfaSetupConfirmContract(IReadOnlyCollection<string> RecoveryCodes);
    public sealed record DisableMfaRequest(string CurrentPassword, string VerificationCode);
    public sealed record RegenerateRecoveryCodesRequest(string CurrentPassword);
    public sealed record RegenerateRecoveryCodesContract(IReadOnlyCollection<string> RecoveryCodes);
}

` 

## src/Services/Identity/Skolio.Identity.Api/Controllers/ParentStudentLinksController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.ParentStudentLinks;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/parent-student-links")]
public sealed class ParentStudentLinksController(IMediator mediator, IdentityDbContext dbContext, ILogger<ParentStudentLinksController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ParentStudentLinkContract>>> List([FromQuery] Guid? parentUserProfileId, [FromQuery] Guid? studentUserProfileId, CancellationToken cancellationToken)
    {
        var query = dbContext.ParentStudentLinks.AsQueryable();

        if (IsParentOnly())
        {
            var actorUserId = SchoolScope.ResolveActorUserId(User);
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.ParentUserProfileId == actorUserId);
        }
        else if (IsStudentOnly())
        {
            var actorUserId = SchoolScope.ResolveActorUserId(User);
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.StudentUserProfileId == actorUserId);
        }
        else
        {
            if (parentUserProfileId.HasValue)
            {
                query = query.Where(x => x.ParentUserProfileId == parentUserProfileId.Value);
            }

            if (studentUserProfileId.HasValue)
            {
                query = query.Where(x => x.StudentUserProfileId == studentUserProfileId.Value);
            }

            if (!SchoolScope.IsPlatformAdministrator(User))
            {
                var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
                if (scopedSchoolIds.Count == 0) return Ok(Array.Empty<ParentStudentLinkContract>());

                var scopedProfileIds = await dbContext.SchoolRoleAssignments
                    .Where(x => scopedSchoolIds.Contains(x.SchoolId))
                    .Select(x => x.UserProfileId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                query = query.Where(x => scopedProfileIds.Contains(x.ParentUserProfileId) || scopedProfileIds.Contains(x.StudentUserProfileId));
            }
        }

        var links = await query.ToListAsync(cancellationToken);
        return Ok(links.Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship)).ToList());
    }

    [HttpGet("me")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ParentStudentLinkContract>>> MyLinks(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var links = await dbContext.ParentStudentLinks
            .Where(x => IsStudentOnly() ? x.StudentUserProfileId == actorUserId : x.ParentUserProfileId == actorUserId)
            .ToListAsync(cancellationToken);

        return Ok(links.Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship)).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ParentStudentLinkContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        if (IsParentOnly())
        {
            var actorUserId = SchoolScope.ResolveActorUserId(User);
            if (actorUserId == Guid.Empty || entity.ParentUserProfileId != actorUserId) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = SchoolScope.ResolveActorUserId(User);
            if (actorUserId == Guid.Empty || entity.StudentUserProfileId != actorUserId) return Forbid();
        }
        else if (!await HasLinkAccess(entity, cancellationToken))
        {
            return Forbid();
        }

        return Ok(new ParentStudentLinkContract(entity.Id, entity.ParentUserProfileId, entity.StudentUserProfileId, entity.Relationship));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<ParentStudentLinkContract>> Create([FromBody] CreateParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        if (!await HasUserScopeAccess(request.ParentUserProfileId, cancellationToken) || !await HasUserScopeAccess(request.StudentUserProfileId, cancellationToken)) return Forbid();

        var result = await mediator.Send(new CreateParentStudentLinkCommand(request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship), cancellationToken);
        Audit("identity.parent-student-link.changed", result.Id, new { request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship, operation = "create" });
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ParentStudentLinkContract>> Override(Guid id, [FromBody] OverrideParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.UpdateRelationship(request.Relationship);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.parent-student-link.override", id, new { request.Relationship, request.OverrideReason });
        return Ok(new ParentStudentLinkContract(entity.Id, entity.ParentUserProfileId, entity.StudentUserProfileId, entity.Relationship));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!await HasLinkAccess(entity, cancellationToken)) return Forbid();

        dbContext.ParentStudentLinks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        Audit("identity.parent-student-link.changed", id, new { entity.ParentUserProfileId, entity.StudentUserProfileId, operation = "delete" });
        return NoContent();
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Student");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private async Task<bool> HasLinkAccess(Skolio.Identity.Domain.Entities.ParentStudentLink link, CancellationToken cancellationToken)
        => await HasUserScopeAccess(link.ParentUserProfileId, cancellationToken) || await HasUserScopeAccess(link.StudentUserProfileId, cancellationToken);

    private async Task<bool> HasUserScopeAccess(Guid userProfileId, CancellationToken cancellationToken)
    {
        if (SchoolScope.IsPlatformAdministrator(User)) return true;

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
        if (scopedSchoolIds.Count == 0) return false;

        return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == userProfileId && scopedSchoolIds.Contains(x.SchoolId), cancellationToken);
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record CreateParentStudentLinkRequest(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
    public sealed record OverrideParentStudentLinkRequest(string Relationship, string OverrideReason);
}

` 

## src/Services/Identity/Skolio.Identity.Api/Controllers/SchoolRolesController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Roles;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/school-roles")]
public sealed class SchoolRolesController(IMediator mediator, IdentityDbContext dbContext, ILogger<SchoolRolesController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedRoleCodes = ["PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"];
    private static readonly HashSet<string> SchoolAdministratorManageableRoleCodes = ["Teacher", "Parent", "Student"];

    [HttpGet("student-me")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> StudentAssignments(CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        return Ok(await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> MyAssignments([FromQuery] Guid? schoolId, CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var query = dbContext.SchoolRoleAssignments.Where(x => x.UserProfileId == actorUserId);
        if (schoolId.HasValue)
        {
            if (!SchoolScope.HasSchoolAccess(User, schoolId.Value)) return Forbid();
            query = query.Where(x => x.SchoolId == schoolId.Value);
        }
        else if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.SchoolId));
        }

        return Ok(await query
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> List([FromQuery] Guid? schoolId, [FromQuery] string? roleCode, CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolRoleAssignments.AsQueryable();
        if (schoolId.HasValue)
        {
            if (!SchoolScope.HasSchoolAccess(User, schoolId.Value)) return Forbid();
            query = query.Where(x => x.SchoolId == schoolId.Value);
        }
        else if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.SchoolId));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            query = query.Where(x => x.RoleCode == roleCode);
        }

        return Ok(await query
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        return Ok(new SchoolRoleAssignmentContract(entity.Id, entity.UserProfileId, entity.SchoolId, entity.RoleCode));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        if (!SupportedRoleCodes.Contains(request.RoleCode)) return this.ValidationField("roleCode", "Unsupported role code.");
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (!SchoolScope.IsPlatformAdministrator(User) && !SchoolAdministratorManageableRoleCodes.Contains(request.RoleCode))
        {
            return Forbid();
        }

        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        Audit("identity.role-assignment.changed", result.Id, new { request.UserProfileId, request.SchoolId, request.RoleCode, operation = "assign" });
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (!SchoolScope.IsPlatformAdministrator(User) && !SchoolAdministratorManageableRoleCodes.Contains(entity.RoleCode))
        {
            return Forbid();
        }

        dbContext.SchoolRoleAssignments.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.role-assignment.changed", id, new { entity.UserProfileId, entity.SchoolId, entity.RoleCode, operation = "delete" });
        return NoContent();
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}

` 

## src/Services/Identity/Skolio.Identity.Api/Controllers/UserProfilesController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator, IdentityDbContext dbContext, ILogger<UserProfilesController> logger) : ControllerBase
{
    private static readonly SchoolPositionOptionContract[] SchoolAdministratorPositionOptions =
    [
        new("SCHOOL_ADMINISTRATOR", "School Administrator"),
        new("DEPUTY_SCHOOL_ADMINISTRATOR", "Deputy School Administrator")
    ];

    private static readonly SchoolPositionOptionContract[] TeacherPositionOptions =
    [
        new("TEACHER", "Teacher"),
        new("CLASS_TEACHER", "Class Teacher"),
        new("SUBJECT_TEACHER", "Subject Teacher")
    ];

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> Me(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpGet("me/summary")]
    [Authorize]
    public async Task<ActionResult<MyProfileSummaryContract>> MySummary(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        var parentStudentLinks = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId || x.StudentUserProfileId == actorUserId)
            .OrderBy(x => x.ParentUserProfileId)
            .ThenBy(x => x.StudentUserProfileId)
            .Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship))
            .ToListAsync(cancellationToken);

        var schoolIds = roleAssignments.Select(x => x.SchoolId).Distinct().ToList();

        return Ok(new MyProfileSummaryContract(
            ToContract(profile),
            roleAssignments,
            parentStudentLinks,
            schoolIds,
            SchoolScope.IsPlatformAdministrator(User),
            User.IsInRole("SchoolAdministrator"),
            User.IsInRole("Teacher"),
            User.IsInRole("Parent"),
            User.IsInRole("Student")));
    }

    [HttpGet("me/school-position-options")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<SchoolPositionOptionContract>>> MySchoolPositionOptions([FromQuery] Guid? schoolId, CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        if (SchoolScope.IsParent(User) || SchoolScope.IsStudent(User)) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var assignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .Select(x => new { x.SchoolId, x.RoleCode })
            .ToListAsync(cancellationToken);

        var resolvedSchoolId = schoolId ?? assignments.Select(x => x.SchoolId).FirstOrDefault();
        if (resolvedSchoolId == Guid.Empty) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var roleCodes = assignments
            .Where(x => x.SchoolId == resolvedSchoolId)
            .Select(x => x.RoleCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleCodes.Count == 0) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var options = BuildSchoolPositionOptions(roleCodes);
        return Ok(options);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeSelfRequest(profile, request);
        var positionTitleChanged = !string.Equals(
            profile.PositionTitle?.Trim(),
            normalizedRequest.PositionTitle?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (positionTitleChanged && !string.IsNullOrWhiteSpace(normalizedRequest.PositionTitle))
        {
            var allowedCodes = await ResolveAllowedSchoolPositionCodesForActor(profile.Id, cancellationToken);
            if (!allowedCodes.Contains(normalizedRequest.PositionTitle))
            {
                return this.ValidationField("positionTitle", "Selected school position is not allowed for current school context.");
            }
        }

        var changedFields = CollectChangedFields(profile, normalizedRequest);

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.PositionTitle,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote), cancellationToken);

        Audit("identity.user-profile.self-updated", profile.Id, new { changedFields });
        return Ok(result);
    }

    [HttpGet("linked-students")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> LinkedStudents(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Parent")) return Forbid();

        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var linkedStudentIds = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId)
            .Select(x => x.StudentUserProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (linkedStudentIds.Count == 0) return Ok(Array.Empty<UserProfileContract>());

        var result = await dbContext.UserProfiles
            .Where(x => linkedStudentIds.Contains(x.Id))
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.PositionTitle,
                x.PublicContactNote,
                x.PreferredContactNote))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("student-context")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<StudentContextContract>> StudentContext(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        return Ok(new StudentContextContract(ToContract(profile), roleAssignments));
    }

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> List([FromQuery] bool? isActive, [FromQuery] UserType? userType, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.UserProfiles.AsQueryable();

        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            var scopedProfileIds = await dbContext.SchoolRoleAssignments
                .Where(x => scopedSchoolIds.Contains(x.SchoolId))
                .Select(x => x.UserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => scopedProfileIds.Contains(x.Id));
        }

        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (userType.HasValue) query = query.Where(x => x.UserType == userType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.FirstName, $"%{term}%") || EF.Functions.ILike(x.LastName, $"%{term}%") || EF.Functions.ILike(x.Email, $"%{term}%"));
        }

        var result = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.PositionTitle,
                x.PublicContactNote,
                x.PreferredContactNote))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Update(Guid id, [FromBody] UpdateAdminProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeAdminRequest(request);
        var changedFields = CollectChangedFields(profile, normalizedRequest);

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.PositionTitle,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote), cancellationToken);

        Audit("identity.user-profile.admin-updated", id, new { changedFields });
        return Ok(result);
    }

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> SetActivation(Guid id, [FromBody] SetActivationRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        if (request.IsActive) profile.Activate(); else profile.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "identity.user-profile.activated" : "identity.user-profile.deactivated", id, new { request.IsActive });
        return Ok(ToContract(profile));
    }

    private async Task<bool> HasProfileAccess(Guid profileId, CancellationToken cancellationToken)
    {
        if (SchoolScope.IsPlatformAdministrator(User)) return true;

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
        if (scopedSchoolIds.Count == 0) return false;

        return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profileId && scopedSchoolIds.Contains(x.SchoolId), cancellationToken);
    }

    private UpdateMyProfileRequest NormalizeSelfRequest(UserProfile profile, UpdateMyProfileRequest request)
    {
        var isStudentOnly = User.IsInRole("Student")
            && !User.IsInRole("Teacher")
            && !User.IsInRole("Parent")
            && !User.IsInRole("SchoolAdministrator")
            && !User.IsInRole("PlatformAdministrator");

        var canEditName = !isStudentOnly;
        var canEditPositionTitle = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Teacher");
        var canEditPublicContactNote = User.IsInRole("Teacher");
        var canEditPreferredContactNote = User.IsInRole("Parent");

        return request with
        {
            FirstName = canEditName ? request.FirstName : profile.FirstName,
            LastName = canEditName ? request.LastName : profile.LastName,
            PositionTitle = canEditPositionTitle ? request.PositionTitle : profile.PositionTitle,
            PublicContactNote = canEditPublicContactNote ? request.PublicContactNote : profile.PublicContactNote,
            PreferredContactNote = canEditPreferredContactNote ? request.PreferredContactNote : profile.PreferredContactNote
        };
    }

    private UpdateAdminProfileRequest NormalizeAdminRequest(UpdateAdminProfileRequest request)
    {
        if (SchoolScope.IsPlatformAdministrator(User))
        {
            return request;
        }

        return request with
        {
            PublicContactNote = null,
            PreferredContactNote = null
        };
    }

    private static IReadOnlyCollection<string> CollectChangedFields(UserProfile profile, ProfileEditableValues request)
    {
        var changed = new List<string>();

        if (!string.Equals(profile.FirstName, request.FirstName, StringComparison.Ordinal)) changed.Add("firstName");
        if (!string.Equals(profile.LastName, request.LastName, StringComparison.Ordinal)) changed.Add("lastName");
        if (!string.Equals(profile.PreferredDisplayName, request.PreferredDisplayName, StringComparison.Ordinal)) changed.Add("preferredDisplayName");
        if (!string.Equals(profile.PreferredLanguage, request.PreferredLanguage, StringComparison.Ordinal)) changed.Add("preferredLanguage");
        if (!string.Equals(profile.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal)) changed.Add("phoneNumber");
        if (!string.Equals(profile.PositionTitle, request.PositionTitle, StringComparison.Ordinal)) changed.Add("positionTitle");
        if (!string.Equals(profile.PublicContactNote, request.PublicContactNote, StringComparison.Ordinal)) changed.Add("publicContactNote");
        if (!string.Equals(profile.PreferredContactNote, request.PreferredContactNote, StringComparison.Ordinal)) changed.Add("preferredContactNote");

        return changed;
    }

    private static UserProfileContract ToContract(UserProfile profile)
        => new(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.UserType,
            profile.Email,
            profile.IsActive,
            profile.PreferredDisplayName,
            profile.PreferredLanguage,
            profile.PhoneNumber,
            profile.PositionTitle,
            profile.PublicContactNote,
            profile.PreferredContactNote);

    private async Task<HashSet<string>> ResolveAllowedSchoolPositionCodesForActor(Guid actorUserId, CancellationToken cancellationToken)
    {
        var roleCodes = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .Select(x => x.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return BuildSchoolPositionOptions(roleCodes)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<SchoolPositionOptionContract> BuildSchoolPositionOptions(IEnumerable<string> roleCodes)
    {
        var set = roleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var options = new List<SchoolPositionOptionContract>();

        if (set.Contains("SchoolAdministrator"))
        {
            options.AddRange(SchoolAdministratorPositionOptions);
        }

        if (set.Contains("Teacher"))
        {
            options.AddRange(TeacherPositionOptions);
        }

        return options
            .DistinctBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);

    public sealed record UpdateMyProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, PositionTitle, PublicContactNote, PreferredContactNote);

    public sealed record UpdateAdminProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, PositionTitle, PublicContactNote, PreferredContactNote);

    public abstract record ProfileEditableValues(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? PublicContactNote,
        string? PreferredContactNote);

    public sealed record SetActivationRequest(bool IsActive);
    public sealed record StudentContextContract(UserProfileContract Profile, IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments);
    public sealed record SchoolPositionOptionContract(string Code, string Label);

    public sealed record MyProfileSummaryContract(
        UserProfileContract Profile,
        IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments,
        IReadOnlyCollection<ParentStudentLinkContract> ParentStudentLinks,
        IReadOnlyCollection<Guid> SchoolIds,
        bool IsPlatformAdministrator,
        bool IsSchoolAdministrator,
        bool IsTeacher,
        bool IsParent,
        bool IsStudent);
}

` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/ClassRoomsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.ClassRooms;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/class-rooms")]
public sealed class ClassRoomsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<ClassRoomsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ClassRoomContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.ClassRooms.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignedClassIds = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .Where(x => x.ClassRoomId.HasValue)
                .Select(x => x.ClassRoomId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => assignedClassIds.Contains(x.Id));
        }
        else if (IsStudentOnly())
        {
            var scopedClassRoomIds = SchoolScope.GetStudentClassRoomIds(User);
            if (scopedClassRoomIds.Count == 0) return Ok(Array.Empty<ClassRoomContract>());
            query = query.Where(x => scopedClassRoomIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.DisplayName).Select(x => new ClassRoomContract(x.Id, x.SchoolId, x.GradeLevelId, x.Code, x.DisplayName)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(ClassRoomContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateClassRoomCommand(request.SchoolId, request.GradeLevelId, request.Code, request.DisplayName), cancellationToken);
        Audit("organization.class-room.created", request.SchoolId, new { contract.Id, request.Code, request.DisplayName });
        return CreatedAtAction(nameof(CreateClassRoom), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ClassRoomContract>> OverrideClassRoom(Guid id, [FromBody] OverrideClassRoomRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var classRoom = await dbContext.ClassRooms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (classRoom is null) return NotFound();

        classRoom.OverrideForPlatformSupport(request.Code, request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.class-room.override", classRoom.SchoolId, new { request.OverrideReason, classRoom.Id, request.Code, request.DisplayName });
        return Ok(new ClassRoomContract(classRoom.Id, classRoom.SchoolId, classRoom.GradeLevelId, classRoom.Code, classRoom.DisplayName));
    }

    private static Guid ResolveActorUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private Guid ResolveActorUserId() => ResolveActorUserId(User);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateClassRoomRequest(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
    public sealed record OverrideClassRoomRequest(string Code, string DisplayName, string OverrideReason);
}


` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/SecondaryFieldsOfStudyController.cs

`$ext
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/secondary-fields-of-study")]
public sealed class SecondaryFieldsOfStudyController(OrganizationDbContext dbContext, ILogger<SecondaryFieldsOfStudyController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SecondaryFieldOfStudyContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.SecondaryFieldsOfStudy
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Code)
            .Select(x => new SecondaryFieldOfStudyContract(x.Id, x.SchoolId, x.Code, x.Name))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<SecondaryFieldOfStudyContract>> Create([FromBody] CreateSecondaryFieldOfStudyRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == request.SchoolId, cancellationToken);
        if (school is null) return NotFound();
        if (school.SchoolType != SchoolType.SecondarySchool) return this.ValidationForm("Field of study is available only for secondary schools.");

        var field = SecondaryFieldOfStudy.Create(Guid.NewGuid(), request.SchoolId, school.SchoolType, request.Code, request.Name);
        await dbContext.SecondaryFieldsOfStudy.AddAsync(field, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.secondary-field-of-study.created", request.SchoolId, new { field.Id, request.Code, request.Name });
        return CreatedAtAction(nameof(List), new { schoolId = request.SchoolId }, new SecondaryFieldOfStudyContract(field.Id, field.SchoolId, field.Code, field.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<SecondaryFieldOfStudyContract>> Update(Guid id, [FromBody] UpdateSecondaryFieldOfStudyRequest request, CancellationToken cancellationToken)
    {
        var field = await dbContext.SecondaryFieldsOfStudy.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (field is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, field.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == field.SchoolId, cancellationToken);
        if (school is null) return NotFound();

        field.Update(school.SchoolType, request.Code, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.secondary-field-of-study.updated", field.SchoolId, new { field.Id, request.Code, request.Name });
        return Ok(new SecondaryFieldOfStudyContract(field.Id, field.SchoolId, field.Code, field.Name));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSecondaryFieldOfStudyRequest(Guid SchoolId, string Code, string Name);
    public sealed record UpdateSecondaryFieldOfStudyRequest(string Code, string Name);
}

` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/SubjectsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Subjects;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/subjects")]
public sealed class SubjectsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<SubjectsController> logger) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<PagedResult<SubjectContract>>> List([FromQuery] Guid schoolId, [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Subjects.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignedSubjectIds = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .Where(x => x.SubjectId.HasValue)
                .Select(x => x.SubjectId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => assignedSubjectIds.Contains(x.Id));
        }
        else if (IsStudentOnly())
        {
            var scopedSubjectIds = SchoolScope.GetStudentSubjectIds(User);
            if (scopedSubjectIds.Count == 0) return Ok(new PagedResult<SubjectContract>(Array.Empty<SubjectContract>(), normalizedPageNumber, normalizedPageSize, 0));
            query = query.Where(x => scopedSubjectIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Code, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new SubjectContract(x.Id, x.SchoolId, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SubjectContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<SubjectContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var isAssigned = await dbContext.TeacherAssignments.AnyAsync(x => x.SchoolId == entity.SchoolId && x.TeacherUserId == actorUserId && x.SubjectId == entity.Id, cancellationToken);
            if (!isAssigned) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var scopedSubjectIds = SchoolScope.GetStudentSubjectIds(User);
            if (!scopedSubjectIds.Contains(entity.Id)) return Forbid();
        }

        return Ok(new SubjectContract(entity.Id, entity.SchoolId, entity.Code, entity.Name));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(SubjectContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateSubjectCommand(request.SchoolId, request.Code, request.Name), cancellationToken);
        Audit("organization.subject.created", request.SchoolId, new { contract.Id, request.Code, request.Name });
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<SubjectContract>> OverrideSubject(Guid id, [FromBody] OverrideSubjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Code, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.subject.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Code, request.Name });
        return Ok(new SubjectContract(entity.Id, entity.SchoolId, entity.Code, entity.Name));
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSubjectRequest(Guid SchoolId, string Code, string Name);
    public sealed record OverrideSubjectRequest(string Code, string Name, string OverrideReason);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");
}


` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/TeacherAssignmentsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeacherAssignments;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teacher-assignments")]
public sealed class TeacherAssignmentsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<TeacherAssignmentsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherAssignmentContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? teacherUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TeacherAssignments.Where(x => x.SchoolId == schoolId);

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            query = query.Where(x => x.TeacherUserId == actorUserId);
        }
        else if (teacherUserId.HasValue)
        {
            query = query.Where(x => x.TeacherUserId == teacherUserId.Value);
        }

        return Ok(await query
            .Select(x => new TeacherAssignmentContract(x.Id, x.SchoolId, x.TeacherUserId, x.Scope, x.ClassRoomId, x.TeachingGroupId, x.SubjectId))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherAssignmentContract>>> MyAssignments([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var result = await dbContext.TeacherAssignments
            .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
            .Select(x => new TeacherAssignmentContract(x.Id, x.SchoolId, x.TeacherUserId, x.Scope, x.ClassRoomId, x.TeachingGroupId, x.SubjectId))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(TeacherAssignmentContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new AssignTeacherCommand(request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId), cancellationToken);
        Audit("organization.teacher-assignment.changed", request.SchoolId, new { contract.Id, request.Scope, request.TeacherUserId, operation = "create" });
        return CreatedAtAction(nameof(AssignTeacher), new { id = contract.Id }, contract);
    }

    [HttpPost("override/reassign")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<TeacherAssignmentContract>> OverrideReassign([FromBody] OverrideReassignTeacherRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        if (request.ExistingAssignmentId.HasValue)
        {
            var existing = await dbContext.TeacherAssignments.FirstOrDefaultAsync(x => x.Id == request.ExistingAssignmentId.Value, cancellationToken);
            if (existing is not null)
            {
                dbContext.TeacherAssignments.Remove(existing);
            }
        }

        var assignment = TeacherAssignment.Create(Guid.NewGuid(), request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId);
        await dbContext.TeacherAssignments.AddAsync(assignment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.teacher-assignment.override", request.SchoolId, new { request.OverrideReason, assignment.Id, request.ExistingAssignmentId });
        return Ok(new TeacherAssignmentContract(assignment.Id, assignment.SchoolId, assignment.TeacherUserId, assignment.Scope, assignment.ClassRoomId, assignment.TeachingGroupId, assignment.SubjectId));
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record AssignTeacherRequest(Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId);
    public sealed record OverrideReassignTeacherRequest(Guid? ExistingAssignmentId, Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId, string OverrideReason);
}

` 

## src/Services/Organization/Skolio.Organization.Api/Controllers/TeachingGroupsController.cs

`$ext
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeachingGroups;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teaching-groups")]
public sealed class TeachingGroupsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<TeachingGroupsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<TeachingGroupContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TeachingGroups.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignments = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .ToListAsync(cancellationToken);

            var groupIds = assignments.Where(x => x.TeachingGroupId.HasValue).Select(x => x.TeachingGroupId!.Value).ToHashSet();
            var classIds = assignments.Where(x => x.ClassRoomId.HasValue).Select(x => x.ClassRoomId!.Value).ToHashSet();

            query = query.Where(x => groupIds.Contains(x.Id) || (x.ClassRoomId.HasValue && classIds.Contains(x.ClassRoomId.Value)));
        }
        else if (IsStudentOnly())
        {
            var scopedGroupIds = SchoolScope.GetStudentTeachingGroupIds(User);
            if (scopedGroupIds.Count == 0) return Ok(Array.Empty<TeachingGroupContract>());
            query = query.Where(x => scopedGroupIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.Name).Select(x => new TeachingGroupContract(x.Id, x.SchoolId, x.ClassRoomId, x.Name, x.IsDailyOperationsGroup)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(TeachingGroupContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeachingGroup([FromBody] CreateTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateTeachingGroupCommand(request.SchoolId, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup), cancellationToken);
        Audit("organization.teaching-group.created", request.SchoolId, new { contract.Id, request.Name, request.IsDailyOperationsGroup });
        return CreatedAtAction(nameof(CreateTeachingGroup), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<TeachingGroupContract>> Override(Guid id, [FromBody] OverrideTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.TeachingGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.ClassRoomId, request.Name, request.IsDailyOperationsGroup);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.teaching-group.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup });
        return Ok(new TeachingGroupContract(entity.Id, entity.SchoolId, entity.ClassRoomId, entity.Name, entity.IsDailyOperationsGroup));
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateTeachingGroupRequest(Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
    public sealed record OverrideTeachingGroupRequest(Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup, string OverrideReason);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");
}


` 

## src/Frontend/Skolio.Frontend/src/shared/http/httpClient.ts

`$ext
import type { SkolioBootstrapConfig } from '../../bootstrap';
import { clearSession, loadSession } from '../auth/session';

export type ValidationProblem = { errors?: Record<string, string[]>; title?: string; status?: number };
export type ValidationErrorMap = { fieldErrors: Record<string, string[]>; formErrors: string[] };

export class SkolioHttpError extends Error {
  constructor(message: string, public status: number, public problem?: ValidationProblem) {
    super(message);
  }
}

export function extractValidationErrors(error: unknown): ValidationErrorMap {
  const empty: ValidationErrorMap = { fieldErrors: {}, formErrors: [] };
  if (!(error instanceof SkolioHttpError) || !error.problem) return empty;
  const entries = Object.entries(error.problem.errors ?? {});
  if (entries.length === 0) {
    return {
      fieldErrors: {},
      formErrors: error.problem.title ? [error.problem.title] : [error.message]
    };
  }

  const fieldErrors: Record<string, string[]> = {};
  const formErrors: string[] = [];
  for (const [field, messages] of entries) {
    if (field === '$form' || field === '' || field === '_') {
      formErrors.push(...messages);
      continue;
    }
    fieldErrors[field] = messages;
  }

  if (formErrors.length === 0 && error.problem.title) {
    formErrors.push(error.problem.title);
  }

  return { fieldErrors, formErrors };
}

export function createHttpClient(config: SkolioBootstrapConfig) {
  const resolveBase = (service: 'identity' | 'organization' | 'academics' | 'communication' | 'administration') => {
    switch (service) {
      case 'identity': return config.identityAuthority;
      case 'organization': return config.organizationApi;
      case 'academics': return config.academicsApi;
      case 'communication': return config.communicationApi;
      default: return config.administrationApi;
    }
  };

  return async function request<T>(service: 'identity' | 'organization' | 'academics' | 'communication' | 'administration', path: string, init?: RequestInit): Promise<T> {
    const session = loadSession();
    const headers = new Headers(init?.headers);
    headers.set('Content-Type', 'application/json');
    if (session) headers.set('Authorization', `Bearer ${session.accessToken}`);

    let response: Response;
    try {
      response = await fetch(`${resolveBase(service)}${path}`, { ...init, headers });
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 300));
      response = await fetch(`${resolveBase(service)}${path}`, { ...init, headers });
    }

    if (response.status === 401) {
      clearSession();
      window.dispatchEvent(new CustomEvent('skolio:auth-expired'));
      window.location.replace('/');
      throw new SkolioHttpError('Session expired', 401);
    }

    if (response.status === 403) {
      throw new SkolioHttpError('Forbidden', 403);
    }

    if (!response.ok) {
      let problem: ValidationProblem | undefined;
      try { problem = (await response.json()) as ValidationProblem; } catch { /* ignore */ }
      throw new SkolioHttpError(problem?.title ?? `Request failed with ${response.status}`, response.status, problem);
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
  };
}
` 

## src/Frontend/Skolio.Frontend/src/identity/IdentityParityPage.tsx

`$ext
import React, { useEffect, useMemo, useState } from 'react';
import type { createIdentityApi, MyProfileSummary, SchoolPositionOption, SelfProfileUpdatePayload, UserProfile } from './api';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi, TeacherAssignment } from '../organization/api';
import { Card, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { localeLabels, supportedLocales, useI18n } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';

type ProfileDraft = SelfProfileUpdatePayload;

const EMPTY_DRAFT: ProfileDraft = {
  firstName: '',
  lastName: '',
  preferredDisplayName: '',
  preferredLanguage: '',
  phoneNumber: '',
  positionTitle: '',
  publicContactNote: '',
  preferredContactNote: ''
};

export function IdentityParityPage({
  api,
  organizationApi,
  session
}: {
  api: ReturnType<typeof createIdentityApi>;
  organizationApi?: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
}) {
  const { t } = useI18n();
  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState('');
  const [formError, setFormError] = useState('');
  const [formSuccess, setFormSuccess] = useState('');
  const [savingSelf, setSavingSelf] = useState(false);
  const [summary, setSummary] = useState<MyProfileSummary | null>(null);
  const [linkedStudents, setLinkedStudents] = useState<UserProfile[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<TeacherAssignment[]>([]);
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [schoolPositionOptions, setSchoolPositionOptions] = useState<SchoolPositionOption[]>([]);
  const [schoolPositionLoading, setSchoolPositionLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selfDraft, setSelfDraft] = useState<ProfileDraft>(EMPTY_DRAFT);
  const [adminDraft, setAdminDraft] = useState<ProfileDraft>(EMPTY_DRAFT);
  const [fieldErrors, setFieldErrors] = useState<{ firstName?: string; lastName?: string }>({});

  const isPlatformAdministrator = session.roles.includes('PlatformAdministrator');
  const isSchoolAdministrator = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher');
  const isParent = session.roles.includes('Parent');
  const isStudentOnly = session.roles.includes('Student')
    && !session.roles.includes('Teacher')
    && !session.roles.includes('Parent')
    && !session.roles.includes('SchoolAdministrator')
    && !session.roles.includes('PlatformAdministrator');

  const canAdminProfiles = summary?.isPlatformAdministrator || summary?.isSchoolAdministrator || false;

  const selfEditRules = useMemo(() => ({
    canEditName: !isStudentOnly,
    canEditSchoolPosition: isSchoolAdministrator || isTeacher || isPlatformAdministrator,
    canEditPublicContactNote: isTeacher,
    canEditPreferredContactNote: isParent
  }), [isParent, isPlatformAdministrator, isSchoolAdministrator, isStudentOnly, isTeacher]);

  const selectedSchoolId = session.schoolIds[0] ?? '';
  const canShowSchoolPositionField = (isSchoolAdministrator || isTeacher || isPlatformAdministrator) && (schoolPositionLoading || schoolPositionOptions.length > 0);

  const mapToDraft = (profile: UserProfile): ProfileDraft => ({
    firstName: profile.firstName ?? '',
    lastName: profile.lastName ?? '',
    preferredDisplayName: profile.preferredDisplayName ?? '',
    preferredLanguage: profile.preferredLanguage ?? '',
    phoneNumber: profile.phoneNumber ?? '',
    positionTitle: profile.positionTitle ?? '',
    publicContactNote: profile.publicContactNote ?? '',
    preferredContactNote: profile.preferredContactNote ?? ''
  });

  const load = () => {
    setLoading(true);
    setPageError('');
    setFormError('');
    setFormSuccess('');
    setFieldErrors({});

    void api.myProfileSummary()
      .then(async (result) => {
        setSummary(result);
        setSelfDraft(mapToDraft(result.profile));

        const tasks: Promise<unknown>[] = [];

        if (isParent) {
          tasks.push(api.linkedStudents().then(setLinkedStudents));
        }

        if (isTeacher && selectedSchoolId && organizationApi) {
          tasks.push(organizationApi.myTeacherAssignments(selectedSchoolId).then(setTeacherAssignments));
        }

        if (result.isPlatformAdministrator || result.isSchoolAdministrator) {
          tasks.push(api.userProfiles().then(setUsers));
        } else {
          setUsers([]);
          setSelectedUserId('');
          setAdminDraft(EMPTY_DRAFT);
        }

        if (selfEditRules.canEditSchoolPosition && selectedSchoolId) {
          setSchoolPositionLoading(true);
          tasks.push(
            api.mySchoolPositionOptions(selectedSchoolId)
              .then(setSchoolPositionOptions)
              .finally(() => setSchoolPositionLoading(false))
          );
        } else {
          setSchoolPositionOptions([]);
          setSchoolPositionLoading(false);
        }

        await Promise.all(tasks);
      })
      .catch((e: Error) => setPageError(mapProfileError(e, t)))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (!formSuccess) return;
    const timer = window.setTimeout(() => setFormSuccess(''), 4000);
    return () => window.clearTimeout(timer);
  }, [formSuccess]);

  useEffect(load, [api, organizationApi, session.accessToken]);

  const saveSelfProfile = () => {
    setFormError('');
    setFormSuccess('');
    const nextFieldErrors: { firstName?: string; lastName?: string } = {};
    if (selfEditRules.canEditName && !selfDraft.firstName.trim()) {
      nextFieldErrors.firstName = t('profileFieldRequired');
    }
    if (selfEditRules.canEditName && !selfDraft.lastName.trim()) {
      nextFieldErrors.lastName = t('profileFieldRequired');
    }
    setFieldErrors(nextFieldErrors);
    if (Object.keys(nextFieldErrors).length > 0) {
      setFormError(t('profileSaveErrorValidation'));
      return;
    }
    setSavingSelf(true);

    const payload: ProfileDraft = {
      ...selfDraft,
      firstName: selfEditRules.canEditName ? selfDraft.firstName : (summary?.profile.firstName ?? ''),
      lastName: selfEditRules.canEditName ? selfDraft.lastName : (summary?.profile.lastName ?? ''),
      positionTitle: selfEditRules.canEditSchoolPosition ? selfDraft.positionTitle : (summary?.profile.positionTitle ?? ''),
      publicContactNote: selfEditRules.canEditPublicContactNote ? selfDraft.publicContactNote : (summary?.profile.publicContactNote ?? ''),
      preferredContactNote: selfEditRules.canEditPreferredContactNote ? selfDraft.preferredContactNote : (summary?.profile.preferredContactNote ?? '')
    };

    void api.updateMyProfile(payload)
      .then(() => {
        setFormSuccess(t('profileSaveSuccess'));
        load();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setSavingSelf(false));
  };

  const loadAdminTarget = (userId: string) => {
    setSelectedUserId(userId);
    if (!userId) {
      setAdminDraft(EMPTY_DRAFT);
      return;
    }

    setFormError('');
    void api.userProfile(userId)
      .then((profile) => setAdminDraft(mapToDraft(profile)))
      .catch((e: Error) => setFormError(mapProfileError(e, t)));
  };

  const saveAdminProfile = () => {
    if (!selectedUserId) return;

    setFormError('');
    setFormSuccess('');
    const payload = isPlatformAdministrator
      ? adminDraft
      : { ...adminDraft, publicContactNote: '', preferredContactNote: '' };

    void api.updateUserProfile(selectedUserId, payload)
      .then(() => {
        setFormSuccess(t('profileAdminSaveSuccess'));
        load();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)));
  };

  if (loading) return <LoadingState text={t('profileLoading')} />;
  if (pageError) return <ErrorState text={pageError} />;
  if (!summary) return <EmptyState text={t('profileNotAvailable')} />;

  const headerInitials = toProfileInitials(
    summary.profile.preferredDisplayName
    || `${summary.profile.firstName} ${summary.profile.lastName}`.trim()
    || summary.profile.email
  );

  return (
    <section className="space-y-4">
      <Card>
        <p className="mb-3 font-semibold text-sm">{t('myProfile.accountOverview')}</p>
        <div className="flex flex-wrap items-start gap-3">
          <span className="sk-profile-avatar !h-12 !w-12 !text-sm" aria-hidden="true">
            {headerInitials}
          </span>
          <div className="min-w-0 flex-1">
            <div className="space-y-3">
              <div className="space-y-1">
                <div className="flex items-center gap-2 text-base font-semibold text-slate-900">
                  <ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span>{summary.profile.preferredDisplayName || `${summary.profile.firstName} ${summary.profile.lastName}`.trim()}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileRoleIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelRole')}:</span>
                  <span>{session.roles.join(", ")}</span>
                </div>
              </div>
              <div className="grid gap-2 md:grid-cols-2">
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileEmailIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('email')}:</span>
                  <span>{summary.profile.email}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileStatusIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelAccountActive')}:</span>
                  <span>{summary.profile.isActive ? t('profileValueYes') : t('profileValueNo')}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelSchoolContext')}:</span>
                  <span>{session.schoolType}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileAssignmentIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelAssignedSchools')}:</span>
                  <span>{summary.schoolIds.length}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('myProfile.personalEdit')}</p>
        {formSuccess ? (
          <FeedbackBanner
            type="success"
            message={formSuccess}
            dismissLabel={t('profileDismiss')}
            onDismiss={() => setFormSuccess('')}
          />
        ) : null}
        {formError ? (
          <FeedbackBanner
            type="error"
            message={formError}
            dismissLabel={t('profileDismiss')}
            onDismiss={() => setFormError('')}
          />
        ) : null}
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
          <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
          <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
          <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
          <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
          {canShowSchoolPositionField ? (
            <SchoolPositionField
              icon={<ProfilePositionIcon className="h-4 w-4 shrink-0 text-slate-500" />}
              label={t('profileFieldSchoolPosition')}
              value={selfDraft.positionTitle ?? ''}
              options={schoolPositionOptions}
              loading={schoolPositionLoading || savingSelf}
              onChange={(value) => setSelfDraft((v) => ({ ...v, positionTitle: value }))}
              loadingText={t('profileSchoolPositionLoading')}
              placeholder={t('profileSelectSchoolPositionPlaceholder')}
              unavailableText={t('profileSchoolPositionUnavailable')}
            />
          ) : null}
          <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPublicContactNote')} value={selfDraft.publicContactNote ?? ''} disabled={!selfEditRules.canEditPublicContactNote || savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
          <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredContactNote')} value={selfDraft.preferredContactNote ?? ''} disabled={!selfEditRules.canEditPreferredContactNote || savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredContactNote: value }))} />
        </div>
        <div className="mt-3 flex gap-2">
          <button className={`sk-btn sk-btn-primary gap-2 ${savingSelf ? 'sk-btn-busy' : ''}`} onClick={saveSelfProfile} type="button" disabled={savingSelf} aria-busy={savingSelf}>
            <SaveDiskIcon className="h-4 w-4 shrink-0" />
            <span>{savingSelf ? t('profileSaving') : t('profileButtonSaveMyProfile')}</span>
          </button>
        </div>
      </Card>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">{t('myProfile.roleAssignments')}</p>
          {summary.roleAssignments.length === 0 ? <EmptyState text={t('profileNoRoleAssignments')} /> : (
            <ul className="sk-list">
              {summary.roleAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.roleCode} | {assignment.schoolId}</li>
              ))}
            </ul>
          )}
        </Card>
        <Card>
          <p className="font-semibold text-sm">{t('myProfile.parentStudentLinks')}</p>
          {summary.parentStudentLinks.length === 0 ? <EmptyState text={t('profileNoParentStudentLinks')} /> : (
            <ul className="sk-list">
              {summary.parentStudentLinks.map((link) => (
                <li key={link.id} className="sk-list-item">{link.parentUserProfileId} {'->'} {link.studentUserProfileId} ({link.relationship})</li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {isTeacher ? (
        <Card>
          <p className="font-semibold text-sm">{t('profileTeacherAssignmentsTitle')}</p>
          {teacherAssignments.length === 0 ? <EmptyState text={t('profileNoTeacherAssignments')} /> : (
            <ul className="sk-list">
              {teacherAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.scope} | {t('profileLabelClass')}: {assignment.classRoomId ?? '-'} | {t('profileLabelGroup')}: {assignment.teachingGroupId ?? '-'} | {t('profileLabelSubject')}: {assignment.subjectId ?? '-'}</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {isParent ? (
        <Card>
          <p className="font-semibold text-sm">{t('profileLinkedStudentsTitle')}</p>
          {linkedStudents.length === 0 ? <EmptyState text={t('profileNoLinkedStudents')} /> : (
            <ul className="sk-list">
              {linkedStudents.map((student) => (
                <li key={student.id} className="sk-list-item">{student.firstName} {student.lastName} ({student.email})</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {canAdminProfiles ? (
        <Card>
          <p className="font-semibold text-sm">{t('profileAdminEditTitle')}</p>
          <p className="mt-1 text-xs text-slate-600">{t('profileAdminEditDescription')}</p>
          <div className="mt-3 grid gap-2 md:grid-cols-2">
            <label className="sk-label" htmlFor="admin-user">{t('profileAdminUserSelectLabel')}</label>
            <select id="admin-user" className="sk-input" value={selectedUserId} onChange={(e) => loadAdminTarget(e.target.value)}>
              <option value="">{t('profileAdminUserSelectPlaceholder')}</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.firstName} {user.lastName} ({user.userType})</option>
              ))}
            </select>
          </div>

          {selectedUserId ? (
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              <Field label={t('profileFieldFirstName')} value={adminDraft.firstName} onChange={(value) => setAdminDraft((v) => ({ ...v, firstName: value }))} />
              <Field label={t('profileFieldLastName')} value={adminDraft.lastName} onChange={(value) => setAdminDraft((v) => ({ ...v, lastName: value }))} />
              <Field label={t('profileFieldPreferredDisplayName')} value={adminDraft.preferredDisplayName ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredDisplayName: value }))} />
              <LanguageField label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={adminDraft.preferredLanguage ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredLanguage: value }))} />
              <Field label={t('profileFieldPhoneNumber')} value={adminDraft.phoneNumber ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, phoneNumber: value }))} />
              <Field label={t('profileFieldPositionTitle')} value={adminDraft.positionTitle ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, positionTitle: value }))} />
              <Field label={t('profileFieldPublicContactNote')} value={adminDraft.publicContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, publicContactNote: value }))} />
              <Field label={t('profileFieldPreferredContactNote')} value={adminDraft.preferredContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredContactNote: value }))} />
            </div>
          ) : null}

          <div className="mt-3">
            <button className="sk-btn sk-btn-primary" disabled={!selectedUserId} onClick={saveAdminProfile} type="button">{t('profileButtonSaveAdminEdit')}</button>
          </div>
        </Card>
      ) : null}
    </section>
  );
}

function Field({
  icon,
  label,
  value,
  onChange,
  disabled = false,
  invalid = false,
  errorText
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  invalid?: boolean;
  errorText?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <input
        className={`sk-input ${invalid ? 'sk-input-invalid' : ''}`}
        value={value}
        disabled={disabled}
        aria-invalid={invalid}
        onChange={(e) => onChange(e.target.value)}
      />
      {errorText ? <span className="text-xs text-red-700">{errorText}</span> : null}
    </div>
  );
}

function LanguageField({
  icon,
  label,
  value,
  placeholder,
  onChange,
  disabled = false
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  placeholder: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <select
        className="sk-input"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">{placeholder}</option>
        {supportedLocales.map((locale) => (
          <option key={locale} value={locale}>
            {localeLabels[locale]} ({locale.toUpperCase()})
          </option>
        ))}
      </select>
    </div>
  );
}

function SchoolPositionField({
  icon,
  label,
  value,
  options,
  loading,
  onChange,
  loadingText,
  placeholder,
  unavailableText
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  options: SchoolPositionOption[];
  loading: boolean;
  onChange: (value: string) => void;
  loadingText: string;
  placeholder: string;
  unavailableText: string;
}) {
  const disabled = loading || options.length === 0;

  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <select
        className="sk-input"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">
          {loading ? loadingText : (options.length > 0 ? placeholder : unavailableText)}
        </option>
        {options.map((option) => (
          <option key={option.code} value={option.code}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

function FeedbackBanner({
  type,
  message,
  dismissLabel,
  onDismiss
}: {
  type: 'success' | 'error';
  message: string;
  dismissLabel: string;
  onDismiss: () => void;
}) {
  return (
    <div className={`sk-feedback-banner mt-3 ${type === 'success' ? 'success' : 'error'}`} role={type === 'error' ? 'alert' : 'status'} aria-live={type === 'error' ? 'assertive' : 'polite'}>
      <span className="text-sm font-medium">{message}</span>
      <button type="button" onClick={onDismiss} className="sk-feedback-dismiss">
        {dismissLabel}
      </button>
    </div>
  );
}

function mapProfileError(error: unknown, t: (key: 'profileSaveErrorInvalidSchoolPosition' | 'profileSaveErrorValidation' | 'profileSaveErrorGeneric') => string) {
  const validation = extractValidationErrors(error);
  const merged = [...validation.formErrors, ...Object.values(validation.fieldErrors).flat()].join(' ').toLowerCase();
  if (merged.includes('position') || merged.includes('school context')) {
    return t('profileSaveErrorInvalidSchoolPosition');
  }
  if (Object.keys(validation.fieldErrors).length > 0 || validation.formErrors.length > 0) {
    return t('profileSaveErrorValidation');
  }
  const normalized = (error instanceof Error ? error.message : '').toLowerCase();
  if (normalized.includes('selected school position is not allowed')) {
    return t('profileSaveErrorInvalidSchoolPosition');
  }
  if (normalized.includes('validation')) {
    return t('profileSaveErrorValidation');
  }
  return t('profileSaveErrorGeneric');
}

function SaveDiskIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M5 4h11l3 3v13H5V4Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M8 4v6h8V4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8 20v-6h8v6" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}

function ProfileIdentityIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M5 20a7 7 0 0 1 14 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileEmailIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="5" width="17" height="14" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="m5.5 7 6.5 5 6.5-5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ProfileRoleIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M12 3 4 7v6c0 4.2 2.4 6.8 8 8 5.6-1.2 8-3.8 8-8V7l-8-4Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="m9.5 12 1.8 1.8 3.4-3.6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ProfileSchoolIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M3 10 12 4l9 6-9 6-9-6Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M6 12v6m6-2v4m6-8v6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileAssignmentIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="4" width="17" height="16" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7 8h10M7 12h7M7 16h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileCardIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="4" y="5" width="16" height="14" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7.5 10.5h9M7.5 14h5.5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileLanguageIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M4 6h10M9 4v2m-3 0c.8 2.6 2.3 4.8 4.5 6.5M5.5 15.5h7M9 15.5l2 4m-2-4-2 4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx="17.5" cy="10.5" r="3.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M17.5 7v7M14 10.5h7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfilePhoneIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M7 4h10v16H7z" stroke="currentColor" strokeWidth="1.8" />
      <path d="M10 7h4M11 17h2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfilePositionIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="7" width="17" height="12" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M9 7V5.5A2.5 2.5 0 0 1 11.5 3h1A2.5 2.5 0 0 1 15 5.5V7" stroke="currentColor" strokeWidth="1.8" />
      <path d="M3.5 12h17" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}

function ProfileContactIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M4 6h16v10H8l-4 3V6Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M8 10h8M8 13h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileStatusIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="m8.5 12.5 2.2 2.2 4.8-5.2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function toProfileInitials(value: string) {
  const parts = value
    .split(/[\s._-]+/)
    .map((x) => x.trim())
    .filter((x) => x.length > 0);

  if (parts.length === 0) return 'SK';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}
` 

## src/Frontend/Skolio.Frontend/src/identity/SecuritySelfServicePage.tsx

`$ext
import React, { useEffect, useState } from 'react';
import type { createIdentityApi, MfaSetupStart, SecuritySummary } from './api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { useI18n } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';

export function SecuritySelfServicePage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const { t } = useI18n();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [summary, setSummary] = useState<SecuritySummary | null>(null);
  const [mfaSetup, setMfaSetup] = useState<MfaSetupStart | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);

  const [changePasswordDraft, setChangePasswordDraft] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [changeEmailDraft, setChangeEmailDraft] = useState({ currentPassword: '', newEmail: '' });
  const [mfaConfirmCode, setMfaConfirmCode] = useState('');
  const [mfaDisableDraft, setMfaDisableDraft] = useState({ currentPassword: '', verificationCode: '' });
  const [mfaRegeneratePassword, setMfaRegeneratePassword] = useState('');

  const load = () => {
    setLoading(true);
    setError('');
    setSuccess('');
    void Promise.all([api.securitySummary(), api.mfaStatus()])
      .then(([securitySummary]) => {
        setSummary(securitySummary);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const submitChangePassword = () => {
    setError('');
    setSuccess('');
    if (!changePasswordDraft.currentPassword || !changePasswordDraft.newPassword || !changePasswordDraft.confirmNewPassword) {
      setError(t('profileFieldRequired'));
      return;
    }
    if (changePasswordDraft.newPassword !== changePasswordDraft.confirmNewPassword) {
      setError(t('validationPasswordConfirmationMismatch'));
      return;
    }
    void api.changePassword(changePasswordDraft)
      .then((response) => {
        setSuccess(response.message);
        setChangePasswordDraft({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const submitChangeEmailRequest = () => {
    setError('');
    setSuccess('');
    if (!changeEmailDraft.currentPassword || !changeEmailDraft.newEmail) {
      setError(t('profileFieldRequired'));
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(changeEmailDraft.newEmail)) {
      setError(t('securityInvalidEmailFormat'));
      return;
    }
    void api.requestEmailChange(changeEmailDraft)
      .then((response) => {
        setSuccess(response.message);
        setChangeEmailDraft({ currentPassword: '', newEmail: '' });
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const startMfaSetup = () => {
    setError('');
    setSuccess('');
    void api.startMfaSetup()
      .then((response) => {
        setMfaSetup(response);
        setRecoveryCodes([]);
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const confirmMfaSetup = () => {
    setError('');
    setSuccess('');
    void api.confirmMfaSetup({ verificationCode: mfaConfirmCode })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaConfirmCode('');
        setMfaSetup(null);
        setSuccess(t('securityMfaEnabledSuccess'));
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const disableMfa = () => {
    setError('');
    setSuccess('');
    void api.disableMfa(mfaDisableDraft)
      .then((response) => {
        setSuccess(response.message);
        setMfaDisableDraft({ currentPassword: '', verificationCode: '' });
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const regenerateCodes = () => {
    setError('');
    setSuccess('');
    void api.regenerateRecoveryCodes({ currentPassword: mfaRegeneratePassword })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaRegeneratePassword('');
        setSuccess(t('securityRecoveryRegeneratedSuccess'));
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const mapValidationMessage = (e: unknown) => {
    const mapped = extractValidationErrors(e);
    if (mapped.formErrors.length > 0) return mapped.formErrors[0];
    const firstField = Object.values(mapped.fieldErrors)[0];
    if (firstField && firstField.length > 0) return firstField[0];
    return e instanceof Error ? e.message : t('errorUnexpected');
  };

  if (loading) return <LoadingState text={t('securityLoading')} />;
  if (error) return <ErrorState text={error} />;
  if (!summary) return <EmptyState text={t('securitySummaryNotAvailable')} />;

  return (
    <section className="space-y-4">
      <div className="flex justify-end">
        <button className="sk-btn sk-btn-secondary" onClick={load} type="button">{t('reloadLabel')}</button>
      </div>

      {success ? (
        <Card className="border-emerald-200 bg-emerald-50 text-emerald-900">
          <p className="text-sm font-medium">{success}</p>
        </Card>
      ) : null}

      <Card>
        <p className="font-semibold text-sm">{t('security.overview')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-2">
          <p className="text-sm">{t('security.currentEmail')}: {summary.currentEmail}</p>
          <p className="text-sm">{t('securityEmailConfirmed')}: {summary.emailConfirmed ? t('securityYes') : t('securityNo')}</p>
          <p className="text-sm">{t('securityMfaEnabled')}: {summary.mfaEnabled ? t('securityYes') : t('securityNo')}</p>
          <p className="text-sm">{t('securityRecoveryCodesLeft')}: {summary.recoveryCodesLeft}</p>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.changePassword')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={changePasswordDraft.currentPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('security.newPassword')} value={changePasswordDraft.newPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, newPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('security.confirmPassword')} value={changePasswordDraft.confirmNewPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, confirmNewPassword: e.target.value }))} />
        </div>
        <div className="mt-2">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangePassword}>{t('securityChangePasswordAction')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.changeEmail')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-2">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={changeEmailDraft.currentPassword} onChange={(e) => setChangeEmailDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" type="email" placeholder={t('security.newEmail')} value={changeEmailDraft.newEmail} onChange={(e) => setChangeEmailDraft((v) => ({ ...v, newEmail: e.target.value }))} />
        </div>
        <div className="mt-2">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangeEmailRequest}>{t('securityRequestEmailChangeAction')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.mfa')}</p>
        <div className="mt-2 flex flex-wrap gap-2">
          <StatusBadge label={summary.mfaEnabled ? t('securityMfaEnabledBadge') : t('securityMfaDisabledBadge')} tone={summary.mfaEnabled ? 'good' : 'warn'} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={startMfaSetup}>{t('security.enableMfa')}</button>
        </div>

        {mfaSetup ? (
          <div className="mt-3 space-y-2 rounded-md border border-slate-200 bg-slate-50 p-3">
            <p className="text-xs text-slate-600">{t('securitySharedKey')}</p>
            <code className="block text-sm">{mfaSetup.sharedKey}</code>
            <p className="text-xs text-slate-600">{t('securityAuthenticatorUri')}</p>
            <code className="block break-all text-xs">{mfaSetup.authenticatorUri}</code>
            <div className="flex gap-2">
              <input className="sk-input" placeholder={t('securityVerificationCode')} value={mfaConfirmCode} onChange={(e) => setMfaConfirmCode(e.target.value)} />
              <button className="sk-btn sk-btn-primary" type="button" onClick={confirmMfaSetup}>{t('securityConfirmMfaAction')}</button>
            </div>
          </div>
        ) : null}

        <div className="mt-3 grid gap-2 md:grid-cols-3">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={mfaDisableDraft.currentPassword} onChange={(e) => setMfaDisableDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" placeholder={t('securityMfaVerificationCode')} value={mfaDisableDraft.verificationCode} onChange={(e) => setMfaDisableDraft((v) => ({ ...v, verificationCode: e.target.value }))} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={disableMfa}>{t('security.disableMfa')}</button>
        </div>

        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={mfaRegeneratePassword} onChange={(e) => setMfaRegeneratePassword(e.target.value)} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={regenerateCodes}>{t('security.regenerateRecoveryCodes')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.recoveryCodes')}</p>
        {recoveryCodes.length === 0 ? <EmptyState text={t('securityRecoveryCodesEmpty')} /> : (
          <ul className="sk-list">
            {recoveryCodes.map((code) => <li className="sk-list-item" key={code}><code>{code}</code></li>)}
          </ul>
        )}
      </Card>
    </section>
  );
}

export function ForgotPasswordPage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const { t } = useI18n();
  const [email, setEmail] = useState('');
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  const submit = () => {
    setError('');
    setDone('');
    void api.forgotPassword({ email })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  };

  return (
    <section className="mx-auto max-w-lg space-y-3 p-4">
      <SectionHeader title={t('securityForgotPasswordTitle')} description={t('securityForgotPasswordDescription')} />
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <input className="sk-input" type="email" placeholder={t('email')} value={email} onChange={(e) => setEmail(e.target.value)} />
          <button className="sk-btn sk-btn-primary" type="button" onClick={submit}>{t('securityForgotPasswordAction')}</button>
        </div>
      </Card>
    </section>
  );
}

export function ResetPasswordPage({
  api,
  userId,
  token
}: {
  api: ReturnType<typeof createIdentityApi>;
  userId: string;
  token: string;
}) {
  const { t } = useI18n();
  const [draft, setDraft] = useState({ newPassword: '', confirmNewPassword: '' });
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  const submit = () => {
    setError('');
    setDone('');
    void api.resetPassword({ userId, token, ...draft })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  };

  return (
    <section className="mx-auto max-w-lg space-y-3 p-4">
      <SectionHeader title={t('securityResetPasswordTitle')} description={t('securityResetPasswordDescription')} />
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <input className="sk-input" type="password" placeholder={t('securityNewPassword')} value={draft.newPassword} onChange={(e) => setDraft((v) => ({ ...v, newPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('securityConfirmNewPassword')} value={draft.confirmNewPassword} onChange={(e) => setDraft((v) => ({ ...v, confirmNewPassword: e.target.value }))} />
          <button className="sk-btn sk-btn-primary" type="button" onClick={submit}>{t('securityResetPasswordAction')}</button>
        </div>
      </Card>
    </section>
  );
}

export function ConfirmEmailChangePage({
  api,
  userId,
  token,
  newEmail
}: {
  api: ReturnType<typeof createIdentityApi>;
  userId: string;
  token: string;
  newEmail: string;
}) {
  const { t } = useI18n();
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    setDone('');
    void api.confirmEmailChange({ userId, token, newEmail })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  }, [api, newEmail, token, userId]);

  if (error) return <section className="mx-auto max-w-lg p-4"><ErrorState text={error} /></section>;
  if (!done) return <section className="mx-auto max-w-lg p-4"><LoadingState text={t('securityConfirmEmailChangeLoading')} /></section>;
  return <section className="mx-auto max-w-lg p-4"><Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card></section>;
}
` 

## src/Frontend/Skolio.Frontend/src/academics/AcademicsParityPage.tsx

`$ext
import React, { useEffect, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createAcademicsApi } from './api';
import type { createAdministrationApi } from '../administration/api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { extractValidationErrors } from '../shared/http/httpClient';
import { useI18n } from '../i18n';

type AcademicsView =
  | 'overview'
  | 'timetable'
  | 'lesson-records'
  | 'attendance'
  | 'excuses'
  | 'grades'
  | 'homework'
  | 'daily-reports';

export function AcademicsParityPage({
  api,
  administrationApi,
  session,
  initialView = 'overview'
}: {
  api: ReturnType<typeof createAcademicsApi>;
  administrationApi: ReturnType<typeof createAdministrationApi>;
  session: SessionState;
  initialView?: AcademicsView;
}) {
  const { t } = useI18n();
  const schoolId = session.schoolIds[0] ?? '';
  const hasSchoolContext = schoolId.length > 0;
  const studentId = session.roles.includes('Parent') ? (session.linkedStudentIds[0] ?? '') : (session.roles.includes('Student') ? session.subject : '');
  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher') && !isSchoolAdmin && !isPlatformAdmin;
  const isParent = session.roles.includes('Parent');
  const isStudent = session.roles.includes('Student') && !isTeacher && !isSchoolAdmin && !isPlatformAdmin;
  const canWritePedagogy = isTeacher || isSchoolAdmin || isPlatformAdmin;
  const canParentExcuse = isParent;
  const [activeView, setActiveView] = useState<AcademicsView>(initialView);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [timetable, setTimetable] = useState<any[]>([]);
  const [lessons, setLessons] = useState<any[]>([]);
  const [attendance, setAttendance] = useState<any[]>([]);
  const [excuses, setExcuses] = useState<any[]>([]);
  const [grades, setGrades] = useState<any[]>([]);
  const [homework, setHomework] = useState<any[]>([]);
  const [dailyReports, setDailyReports] = useState<any[]>([]);
  const [overrideAudit, setOverrideAudit] = useState<any[]>([]);

  const [newTimetable, setNewTimetable] = useState({ schoolYearId: '', dayOfWeek: '1', startTime: '08:00', endTime: '08:45', audienceType: 'ClassRoom', audienceId: '', subjectId: '', teacherUserId: '' });
  const [newLesson, setNewLesson] = useState({ timetableEntryId: '', lessonDate: '', topic: '', summary: '' });
  const [newAttendance, setNewAttendance] = useState({ audienceId: '', studentUserId: '', attendanceDate: '', status: 'Present' });
  const [newExcuse, setNewExcuse] = useState({ attendanceRecordId: '', reason: '' });
  const [excuseFieldErrors, setExcuseFieldErrors] = useState<{ attendanceRecordId?: string; reason?: string }>({});
  const [newGrade, setNewGrade] = useState({ studentUserId: studentId, subjectId: '', gradeValue: '', note: '', gradedOn: '' });
  const [newHomework, setNewHomework] = useState({ audienceId: '', subjectId: '', title: '', instructions: '', dueDate: '' });
  const [newReport, setNewReport] = useState({ audienceId: '', reportDate: '', summary: '', notes: '' });

  const load = () => {
    setLoading(true);
    setError('');
    void Promise.all([
      hasSchoolContext ? api.timetable(schoolId, isStudent ? session.subject : undefined) : Promise.resolve([]),
      hasSchoolContext ? api.lessons(schoolId, undefined, isStudent ? session.subject : undefined) : Promise.resolve([]),
      hasSchoolContext ? api.attendance(schoolId, undefined, studentId || undefined) : Promise.resolve([]),
      canParentExcuse ? api.myExcuses() : (hasSchoolContext ? api.excuses(schoolId, studentId || undefined) : Promise.resolve([])),
      studentId && hasSchoolContext ? api.grades(schoolId, studentId, newGrade.subjectId || '00000000-0000-0000-0000-000000000000').catch(() => []) : Promise.resolve([]),
      hasSchoolContext ? api.homework(schoolId, undefined, studentId || undefined) : Promise.resolve([]),
      hasSchoolContext ? api.dailyReports(schoolId, undefined, studentId || undefined) : Promise.resolve([]),
      isPlatformAdmin ? administrationApi.auditLogs({ actionCode: 'academics.daily-report.override' }) : Promise.resolve([])
    ])
      .then(([tt, lr, at, ex, gr, hw, dr, oa]) => {
        setTimetable(tt);
        setLessons(lr);
        setAttendance(at);
        setExcuses(ex);
        setGrades(gr);
        setHomework(hw);
        setDailyReports(dr);
        setOverrideAudit(oa);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [session.accessToken, newGrade.subjectId]);
  useEffect(() => setActiveView(initialView), [initialView]);

  const onCreateTimetable = () => void api.createTimetableEntry({ id: '', schoolId, ...newTimetable }).then(load).catch((e: Error) => setError(e.message));
  const onCreateLesson = () => void api.createLesson({ id: '', ...newLesson }).then(load).catch((e: Error) => setError(e.message));
  const onCreateAttendance = () => void api.createAttendance({ id: '', schoolId, ...newAttendance }).then(load).catch((e: Error) => setError(e.message));
  const onCreateExcuse = () => {
    const next: { attendanceRecordId?: string; reason?: string } = {};
    if (!newExcuse.attendanceRecordId.trim()) next.attendanceRecordId = t('validationRequiredAttendanceRecordId');
    if (!newExcuse.reason.trim()) next.reason = t('validationRequiredReason');
    setExcuseFieldErrors(next);
    if (Object.keys(next).length > 0) return;
    void api.createMyExcuse(newExcuse)
      .then(() => {
        setNewExcuse({ attendanceRecordId: '', reason: '' });
        load();
      })
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        const message = mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('validationGeneric'));
        setError(message);
      });
  };
  const onCreateGrade = () => void api.createGrade({ id: '', schoolId, ...newGrade }).then(load).catch((e: Error) => setError(e.message));
  const onCreateHomework = () => void api.createHomework({ id: '', schoolId, ...newHomework }).then(load).catch((e: Error) => setError(e.message));
  const onCreateDailyReport = () => void api.createDailyReport({ id: '', schoolId, ...newReport }).then(load).catch((e: Error) => setError(e.message));

  if (loading) return <LoadingState text="Loading academics capabilities..." />;
  if (error) return <ErrorState text={error} />;

  const show = (view: AcademicsView) => activeView === 'overview' || activeView === view;
  const activeViewTitle = activeView === 'overview'
    ? 'Academics Overview'
    : activeView === 'lesson-records'
      ? 'Lesson Records'
      : activeView === 'daily-reports'
        ? 'Daily Reports'
        : activeView.charAt(0).toUpperCase() + activeView.slice(1);

  return (
    <section className="space-y-3">
      <SectionHeader title={activeViewTitle} description="Frontend flows mapped only to existing academics backend endpoints." action={<button className="sk-btn sk-btn-secondary" onClick={load} type="button">Reload</button>} />

      {(canWritePedagogy || canParentExcuse) ? (
        <div className="grid gap-3 lg:grid-cols-2">
          {canWritePedagogy && show('timetable') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create timetable entry</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="School year id" value={newTimetable.schoolYearId} onChange={(e) => setNewTimetable((v) => ({ ...v, schoolYearId: e.target.value }))} />
                  <input className="sk-input" placeholder="Day of week (1-7)" value={newTimetable.dayOfWeek} onChange={(e) => setNewTimetable((v) => ({ ...v, dayOfWeek: e.target.value }))} />
                  <input className="sk-input" placeholder="Audience id" value={newTimetable.audienceId} onChange={(e) => setNewTimetable((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newTimetable.subjectId} onChange={(e) => setNewTimetable((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Teacher user id" value={newTimetable.teacherUserId} onChange={(e) => setNewTimetable((v) => ({ ...v, teacherUserId: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateTimetable} type="button">Create timetable</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('lesson-records') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create lesson record</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Timetable entry id" value={newLesson.timetableEntryId} onChange={(e) => setNewLesson((v) => ({ ...v, timetableEntryId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newLesson.lessonDate} onChange={(e) => setNewLesson((v) => ({ ...v, lessonDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Topic" value={newLesson.topic} onChange={(e) => setNewLesson((v) => ({ ...v, topic: e.target.value }))} />
                  <input className="sk-input" placeholder="Summary" value={newLesson.summary} onChange={(e) => setNewLesson((v) => ({ ...v, summary: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateLesson} type="button">Create lesson</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('attendance') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create attendance</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newAttendance.audienceId} onChange={(e) => setNewAttendance((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Student user id" value={newAttendance.studentUserId} onChange={(e) => setNewAttendance((v) => ({ ...v, studentUserId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newAttendance.attendanceDate} onChange={(e) => setNewAttendance((v) => ({ ...v, attendanceDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Status" value={newAttendance.status} onChange={(e) => setNewAttendance((v) => ({ ...v, status: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateAttendance} type="button">Create attendance</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('grades') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create grade entry</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Student user id" value={newGrade.studentUserId} onChange={(e) => setNewGrade((v) => ({ ...v, studentUserId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newGrade.subjectId} onChange={(e) => setNewGrade((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Grade value" value={newGrade.gradeValue} onChange={(e) => setNewGrade((v) => ({ ...v, gradeValue: e.target.value }))} />
                  <input className="sk-input" placeholder="Note" value={newGrade.note} onChange={(e) => setNewGrade((v) => ({ ...v, note: e.target.value }))} />
                  <input className="sk-input" type="date" value={newGrade.gradedOn} onChange={(e) => setNewGrade((v) => ({ ...v, gradedOn: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateGrade} type="button">Create grade</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('homework') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create homework</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newHomework.audienceId} onChange={(e) => setNewHomework((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newHomework.subjectId} onChange={(e) => setNewHomework((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Title" value={newHomework.title} onChange={(e) => setNewHomework((v) => ({ ...v, title: e.target.value }))} />
                  <input className="sk-input" placeholder="Instructions" value={newHomework.instructions} onChange={(e) => setNewHomework((v) => ({ ...v, instructions: e.target.value }))} />
                  <input className="sk-input" type="date" value={newHomework.dueDate} onChange={(e) => setNewHomework((v) => ({ ...v, dueDate: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateHomework} type="button">Create homework</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('daily-reports') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create daily report</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newReport.audienceId} onChange={(e) => setNewReport((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newReport.reportDate} onChange={(e) => setNewReport((v) => ({ ...v, reportDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Summary" value={newReport.summary} onChange={(e) => setNewReport((v) => ({ ...v, summary: e.target.value }))} />
                  <input className="sk-input" placeholder="Notes" value={newReport.notes} onChange={(e) => setNewReport((v) => ({ ...v, notes: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateDailyReport} type="button">Create daily report</button></div>
              </Card>
            </>
          ) : null}

          {canParentExcuse && show('excuses') ? (
            <Card>
              <p className="font-semibold text-sm">Submit excuse request</p>
              <div className="mt-2 grid gap-2">
                <input className={`sk-input ${excuseFieldErrors.attendanceRecordId ? 'sk-input-invalid' : ''}`} placeholder="Attendance record id" value={newExcuse.attendanceRecordId} onChange={(e) => { setExcuseFieldErrors((v) => ({ ...v, attendanceRecordId: undefined })); setNewExcuse((v) => ({ ...v, attendanceRecordId: e.target.value })); }} />
                {excuseFieldErrors.attendanceRecordId ? <span className="text-xs text-red-700">{excuseFieldErrors.attendanceRecordId}</span> : null}
                <input className={`sk-input ${excuseFieldErrors.reason ? 'sk-input-invalid' : ''}`} placeholder="Reason" value={newExcuse.reason} onChange={(e) => { setExcuseFieldErrors((v) => ({ ...v, reason: undefined })); setNewExcuse((v) => ({ ...v, reason: e.target.value })); }} />
                {excuseFieldErrors.reason ? <span className="text-xs text-red-700">{excuseFieldErrors.reason}</span> : null}
              </div>
              <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateExcuse} type="button">Create excuse</button></div>
            </Card>
          ) : null}
        </div>
      ) : null}

      <div className="grid gap-3 lg:grid-cols-2">
        {show('timetable') ? <Card><p className="font-semibold text-sm">Timetable</p>{timetable.length === 0 ? <EmptyState text="No timetable entries in scope." /> : <ul className="sk-list">{timetable.map((x) => <li className="sk-list-item" key={x.id}>{x.dayOfWeek} {x.startTime}-{x.endTime}</li>)}</ul>}</Card> : null}
        {show('lesson-records') ? <Card><p className="font-semibold text-sm">Lesson records</p>{lessons.length === 0 ? <EmptyState text="No lesson records in scope." /> : <ul className="sk-list">{lessons.map((x) => <li className="sk-list-item" key={x.id}>{x.lessonDate} | {x.topic}</li>)}</ul>}</Card> : null}
        {show('attendance') ? <Card><p className="font-semibold text-sm">Attendance</p>{attendance.length === 0 ? <EmptyState text="No attendance records in scope." /> : <ul className="sk-list">{attendance.map((x) => <li className="sk-list-item" key={x.id}><span>{x.attendanceDate} | {x.studentUserId}</span><StatusBadge label={x.status} tone={x.status === 'Present' ? 'good' : 'warn'} /></li>)}</ul>}</Card> : null}
        {show('excuses') ? <Card><p className="font-semibold text-sm">Excuses</p>{excuses.length === 0 ? <EmptyState text="No excuse records in scope." /> : <ul className="sk-list">{excuses.map((x) => <li className="sk-list-item gap-2" key={x.id}><span className="flex-1">{x.reason}</span>{canParentExcuse ? <><button className="sk-btn sk-btn-secondary" type="button" onClick={() => { const updated = window.prompt('Update excuse reason', x.reason); if (updated && updated.trim().length > 0) { void api.updateMyExcuse(x.id, { reason: updated.trim() }).then(load).catch((e: Error) => setError(e.message)); } }}>Edit</button><button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.cancelMyExcuse(x.id).then(load).catch((e: Error) => setError(e.message))}>Cancel</button><StatusBadge label={new Date(x.submittedAtUtc).toLocaleString()} tone="info" /></> : <StatusBadge label="Read" tone="info" />}</li>)}</ul>}</Card> : null}
        {show('grades') ? <Card><p className="font-semibold text-sm">Grades</p>{grades.length === 0 ? <EmptyState text="No grades in scope." /> : <ul className="sk-list">{grades.map((x) => <li className="sk-list-item" key={x.id}>{x.gradedOn} | {x.gradeValue}</li>)}</ul>}</Card> : null}
        {show('homework') ? <Card><p className="font-semibold text-sm">Homework</p>{homework.length === 0 ? <EmptyState text="No homework in scope." /> : <ul className="sk-list">{homework.map((x) => <li className="sk-list-item" key={x.id}>{x.title}</li>)}</ul>}</Card> : null}
        {show('daily-reports') ? <Card className="lg:col-span-2"><p className="font-semibold text-sm">Daily reports</p>{dailyReports.length === 0 ? <EmptyState text="No daily reports in scope." /> : <ul className="sk-list">{dailyReports.map((x) => <li className="sk-list-item" key={x.id}>{x.reportDate} | {x.summary}</li>)}</ul>}</Card> : null}
      </div>

      {isPlatformAdmin ? (
        <Card>
          <p className="font-semibold text-sm">Override audit summary</p>
          {overrideAudit.length === 0 ? <EmptyState text="No override audit entries." /> : (
            <ul className="sk-list">{overrideAudit.map((x) => <li className="sk-list-item" key={x.id}>{x.actionCode}</li>)}</ul>
          )}
        </Card>
      ) : null}
    </section>
  );
}
` 

## src/Frontend/Skolio.Frontend/src/i18n.tsx

`$ext
import React, { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

export type Locale = 'cs' | 'sk' | 'de' | 'pl' | 'en';

const STORAGE_KEY = 'skolio.locale';

const localeLabelsInternal: Record<Locale, string> = {
  cs: 'Čeština',
  en: 'English',
  sk: 'Slovenčina',
  de: 'Deutsch',
  pl: 'Polski'
};

const en = {
  appTitle: 'Skolio App Shell',
  platform: 'Platform',
  signIn: 'Sign in',
  signOut: 'Sign out',
  loginTitle: 'Sign in to Skolio',
  loginSubtitle: 'Use your credentials.',
  email: 'Email',
  password: 'Password',
  goToLogin: 'Go to login',
  landingTitle: 'Skolio',
  landingSubtitle: 'Modern platform for schools. Organization, academics, communication, and administration in one place.',
  landingHeroTag: 'Digital platform for schools',
  landingHeroTitle: 'Skolio connects academics, communication and administration in one place',
  landingHeroText: 'From kindergarten to secondary school. Clear workflows for leaders, teachers and parents without switching between disconnected tools.',
  landingStat1: 'Schools managed in one platform',
  landingStat2: 'Fast rollout and onboarding',
  landingStat3: 'Unified sign-in and role model',
  landingModulesTitle: 'Platform modules',
  landingModulesText: 'Each module solves a concrete area of school operations while sharing one identity and one data backbone.',
  landingProcessTitle: 'How it works',
  landingProcess1Title: '1. School setup',
  landingProcess1Text: 'Configure organization, roles, classes and school structure in a guided flow.',
  landingProcess2Title: '2. Daily operations',
  landingProcess2Text: 'Schedule, attendance, grades, homework and announcements in one workspace.',
  landingProcess3Title: '3. Governance',
  landingProcess3Text: 'Administration, audit trail and clear accountability across the whole stack.',
  landingCtaTitle: 'Ready to start?',
  landingCtaText: 'Open the sign-in screen and enter your Skolio workspace.',
  landingFooter: 'Skolio platform for modern schools',
  featureOrganizationTitle: 'Organization',
  featureOrganizationText: 'Schools, classes, roles and organizational structure.',
  featureAcademicsTitle: 'Academics',
  featureAcademicsText: 'Schedule, attendance, grades, assignments and daily agenda.',
  featureCommunicationTitle: 'Communication',
  featureCommunicationText: 'Announcements and communication between school, teachers and parents.',
  routeDashboard: 'Dashboard',
  routeOrganization: 'Organization',
  routeAcademics: 'Academics',
  routeCommunication: 'Communication',
  routeAdministration: 'Administration',
  routeIdentity: 'Identity',
  routeSecurity: 'Security',
  'security.title': 'Security',
  'security.overview': 'Security overview',
  'security.changePassword': 'Change password',
  'security.changeEmail': 'Change email',
  'security.mfa': 'Two-factor authentication',
  'security.recoveryCodes': 'Recovery codes',
  'security.currentPassword': 'Current password',
  'security.newPassword': 'New password',
  'security.confirmPassword': 'Confirm new password',
  'security.currentEmail': 'Current email',
  'security.newEmail': 'New email address',
  'security.enableMfa': 'Enable two-factor authentication',
  'security.disableMfa': 'Disable two-factor authentication',
  'security.regenerateRecoveryCodes': 'Generate new recovery codes',
  securityTitle: 'Security',
  securityDescription: 'Identity security self-service separated from business profile.',
  securitySummaryTitle: 'Security summary',
  securityCurrentEmail: 'Current email',
  securityEmailConfirmed: 'Email confirmed',
  securityMfaEnabled: 'MFA enabled',
  securityRecoveryCodesLeft: 'Recovery codes left',
  securityYes: 'Yes',
  securityNo: 'No',
  securityChangePasswordTitle: 'Change password',
  securityCurrentPassword: 'Current password',
  securityNewPassword: 'New password',
  securityConfirmNewPassword: 'Confirm new password',
  securityChangePasswordAction: 'Change password',
  securityChangeEmailTitle: 'Change email',
  securityCurrentPasswordReauth: 'Current password (re-auth)',
  securityNewEmail: 'New email',
  securityRequestEmailChangeAction: 'Request email change',
  securityMfaManagementTitle: 'MFA management (TOTP + recovery codes)',
  securityMfaEnabledBadge: 'MFA enabled',
  securityMfaDisabledBadge: 'MFA disabled',
  securityStartMfaSetupAction: 'Start MFA setup',
  securitySharedKey: 'Shared key',
  securityAuthenticatorUri: 'Authenticator URI',
  securityVerificationCode: 'Verification code',
  securityConfirmMfaAction: 'Confirm MFA',
  securityMfaVerificationCode: 'MFA verification code',
  securityDisableMfaAction: 'Disable MFA',
  securityRegenerateRecoveryCodesAction: 'Regenerate recovery codes',
  securityRecoveryCodesTitle: 'Recovery codes',
  securityRecoveryCodesEmpty: 'Recovery codes are shown only after setup confirmation or regeneration.',
  securityLoading: 'Loading security self-service...',
  securitySummaryNotAvailable: 'Security summary is not available.',
  securityMfaEnabledSuccess: 'MFA enabled.',
  securityRecoveryRegeneratedSuccess: 'Recovery codes regenerated.',
  securityForgotPasswordTitle: 'Forgot password',
  securityForgotPasswordDescription: 'Request password reset email. Response is generic for security reasons.',
  securityForgotPasswordAction: 'Send reset email',
  securityResetPasswordTitle: 'Reset password',
  securityResetPasswordDescription: 'Complete password reset using secure token.',
  securityResetPasswordAction: 'Reset password',
  securityConfirmEmailChangeLoading: 'Confirming email change...',
  securityMissingResetParams: 'Missing reset password token parameters.',
  securityMissingEmailConfirmParams: 'Missing email confirmation parameters.',
  roleUser: 'User',
  dashboardSuffix: 'dashboard',
  dashboardKindergarten: 'Groups, daily reports, attendance and parent communication.',
  dashboardSecondary: 'Classes, subjects, study programs and broader school agenda.',
  dashboardDefault: 'Classes, subjects, schedule, attendance, grades and homework.',
  loadingOrganization: 'Loading organization view...',
  organizationTitle: 'Organization',
  academicsTitle: 'Academics',
  academicsKindergartenHint: 'Daily report workflow is emphasized.',
  academicsDefaultHint: 'Schedule, lessons, attendance, grades and homework.',
  loadDailyReports: 'Load daily reports',
  communicationTitle: 'Communication',
  connectionState: 'Connection',
  connected: 'connected',
  disconnected: 'disconnected',
  retrying: 'retrying',
  reload: 'Reload',
  administrationTitle: 'Administration',
  systemSettings: 'System settings',
  auditLog: 'Audit log',
  identityTitle: 'Identity',
  unauthorizedAdministration: 'You are not authorized for administration route.',
  unauthorizedIdentity: 'You are not authorized for identity profile details.',
  unauthorizedCommon: 'You are not authorized for this view.',
  processingCallback: 'Processing callback...',
  authCompleted: 'Authentication completed. Redirecting...',
  authFailed: 'Authentication failed',
  missingAuthState: 'Missing authorization code state.',
  stateValidationFailed: 'State validation failed.',
  tokenExchangeFailed: 'Token exchange failed with {status}',
  shellSubtitle: 'Role-based and school-type-aware application shell.',
  shellApplication: 'Application shell',
  language: 'Language',
  selectLanguage: 'Select language',
  notifications: 'Notifications',
  mainNavigation: 'Main navigation',
  openProfileMenu: 'Open profile menu',
  close: 'Close',
  menu: 'Menu',
  myProfile: 'My Profile',
  'myProfile.title': 'My Profile',
  'myProfile.accountOverview': 'Account overview',
  'myProfile.personalEdit': 'Self profile edit',
  'myProfile.roleAssignments': 'Assigned roles',
  'myProfile.parentStudentLinks': 'Parent-student links',
  profile: 'Profile',
  signOutMenu: 'Sign Out',
  overview: 'Overview',
  operationsKindergarten: 'Operations (Groups & Daily Reports)',
  operationsElementary: 'Operations (Classes & Subjects)',
  operationsSecondary: 'Operations (Classes, Subjects, Study Context)',
  navSchools: 'Schools',
  navSchoolYears: 'School Years',
  navGradeLevels: 'Grade Levels',
  navClasses: 'Classes',
  navGroups: 'Groups',
  navSubjects: 'Subjects',
  navFieldsOfStudy: 'Fields of Study',
  navTeacherAssignments: 'Teacher Assignments',
  navTimetable: 'Timetable',
  navLessonRecords: 'Lesson Records',
  navAttendance: 'Attendance',
  navExcuses: 'Excuses',
  navGrades: 'Grades',
  navHomework: 'Homework',
  navDailyReports: 'Daily Reports',
  create: 'Create',
  save: 'Save',
  cancel: 'Cancel',
  reloadLabel: 'Reload',
  open: 'Open',
  send: 'Send',
  publish: 'Publish',
  activate: 'Activate',
  deactivate: 'Deactivate',
  loading: 'Loading...',
  noData: 'No data in current scope.',
  noRecords: 'No records in current scope.',
  stateActive: 'Active',
  stateInactive: 'Inactive',
  stateRead: 'Read',
  profileHeaderTitle: 'Profile',
  profileHeaderDescription: 'Business profile self-service with strict read-only boundaries for identity assignments and links.',
  profileLabelRole: 'Role',
  profileLabelSchoolContext: 'School context',
  profileLabelAssignedSchools: 'Assigned schools',
  profileLabelClass: 'Class',
  profileLabelGroup: 'Group',
  profileLabelSubject: 'Subject',
  profileLabelAccountActive: 'Account active',
  profileValueYes: 'Yes',
  profileValueNo: 'No',
  profileSaveSuccess: 'Profile saved.',
  profileAdminSaveSuccess: 'Administrative profile edit saved.',
  profileSaving: 'Saving profile...',
  profileSaveErrorGeneric: 'Profile could not be saved. Try again.',
  profileSaveErrorValidation: 'Please fix highlighted fields.',
  profileSaveErrorInvalidSchoolPosition: 'Selected school position is not valid for your school context.',
  profileFieldRequired: 'This field is required.',
  profileDismiss: 'Dismiss',
  profileLoading: 'Loading profile...',
  profileNotAvailable: 'Profile is not available.',
  profileSelfEditTitle: 'Self profile edit',
  profileReadOnlyHint: 'Email / Username / Role assignments are read-only',
  profileFieldFirstName: 'First name',
  profileFieldLastName: 'Last name',
  profileFieldPreferredDisplayName: 'Preferred display name',
  profileFieldPreferredLanguage: 'Preferred language',
  profileFieldPhoneNumber: 'Phone number',
  profileFieldPositionTitle: 'Position title',
  profileFieldSchoolPosition: 'School position',
  profileSelectSchoolPositionPlaceholder: 'Select school position',
  profileSchoolPositionLoading: 'Loading school positions...',
  profileSchoolPositionUnavailable: 'No school positions available for current school context.',
  profileFieldPublicContactNote: 'Public contact note',
  profileFieldPreferredContactNote: 'Preferred contact note',
  profileButtonSaveMyProfile: 'Save my profile',
  profileRoleAssignmentsTitle: 'Role assignments',
  profileNoRoleAssignments: 'No role assignments.',
  profileParentStudentLinksTitle: 'Parent-student links',
  profileNoParentStudentLinks: 'No parent-student links.',
  profileTeacherAssignmentsTitle: 'Teacher assignments',
  profileNoTeacherAssignments: 'No teacher assignments in selected school context.',
  profileLinkedStudentsTitle: 'Linked students summary',
  profileNoLinkedStudents: 'No linked students.',
  profileAdminEditTitle: 'Administrative profile edit',
  profileAdminEditDescription: 'Role assignments remain in dedicated role-management boundary and are not edited here.',
  profileAdminUserSelectLabel: 'User profile',
  profileAdminUserSelectPlaceholder: 'Select user',
  profileButtonSaveAdminEdit: 'Save administrative edit',
  profileSelectLanguagePlaceholder: 'Select language',
  validationPasswordConfirmationMismatch: 'Password confirmation does not match.',
  securityInvalidEmailFormat: 'Invalid email format.',
  validationRequiredAttendanceRecordId: 'Attendance record id is required.',
  validationRequiredReason: 'Reason is required.',
  validationGeneric: 'Validation failed.',
  errorForbidden: 'You are not authorized to access this page.',
  errorNotFound: 'Requested page was not found.',
  errorUnexpected: 'Unexpected error occurred.'
} as const;

type TranslationKey = keyof typeof en;

type Translations = Record<TranslationKey, string>;

const cs: Translations = {
  ...en,
  appTitle: 'Skolio Aplikace',
  platform: 'Platforma',
  signIn: 'P\u0159ihl\u00E1sit se',
  signOut: 'Odhl\u00E1sit se',
  loginTitle: 'P\u0159ihl\u00E1\u0161en\u00ED do Skolio',
  loginSubtitle: 'Pou\u017Eijte sv\u00E9 p\u0159ihla\u0161ovac\u00ED \u00FAdaje.',
  goToLogin: 'P\u0159ej\u00EDt na p\u0159ihl\u00E1\u0161en\u00ED',
  landingSubtitle: 'Modern\u00ED platforma pro \u0161koly. Organizace, studijn\u00ED agenda, komunikace a administrace na jednom m\u00EDst\u011B.',
  landingHeroTag: 'Digit\u00E1ln\u00ED platforma pro \u0161koly',
  landingHeroTitle: 'Skolio propojuje v\u00FDuku, komunikaci a administraci na jednom m\u00EDst\u011B',
  landingHeroText: 'Od mate\u0159sk\u00E9 \u0161koly po st\u0159edn\u00ED \u0161kolu. P\u0159ehledn\u00E1 agenda pro veden\u00ED, u\u010Ditele i rodi\u010De, bez zbyte\u010Dn\u00FDch p\u0159ep\u00EDn\u00E1n\u00ED mezi syst\u00E9my.',
  landingStat1: '\u0160koly pod jednou spr\u00E1vou',
  landingStat2: 'Rychl\u00E9 nasazen\u00ED a onboarding',
  landingStat3: 'Jednotn\u00E9 p\u0159ihl\u00E1\u0161en\u00ED a role',
  landingModulesTitle: 'Moduly platformy',
  landingModulesText: 'Ka\u017Ed\u00FD modul \u0159e\u0161\u00ED konkr\u00E9tn\u00ED oblast provozu \u0161koly, ale v\u0161e sd\u00EDl\u00ED jednu identitu a jednotn\u00E1 data.',
  landingProcessTitle: 'Jak to funguje',
  landingProcess1Title: '1. Nastaven\u00ED \u0161koly',
  landingProcess1Text: 'Zalo\u017Een\u00ED organizace, rol\u00ED, t\u0159\u00EDd a struktury \u0161koly b\u011Bhem n\u011Bkolika krok\u016F.',
  landingProcess2Title: '2. Denn\u00ED provoz',
  landingProcess2Text: 'Rozvrh, doch\u00E1zka, zn\u00E1mky, \u00FAkoly i ozn\u00E1men\u00ED v jednom pracovn\u00EDm prostoru.',
  landingProcess3Title: '3. P\u0159ehled a kontrola',
  landingProcess3Text: 'Administrace, auditn\u00ED stopa a jasn\u00E1 odpov\u011Bdnost v r\u00E1mci cel\u00E9ho stacku.',
  landingCtaTitle: 'P\u0159ipraveni za\u010D\u00EDt?',
  landingCtaText: 'P\u0159ejd\u011Bte na p\u0159ihl\u00E1\u0161en\u00ED a otev\u0159ete sv\u00E9 pracovn\u00ED prost\u0159ed\u00ED ve Skolio.',
  landingFooter: 'Skolio platforma pro modern\u00ED \u0161kolu',
  featureOrganizationTitle: 'Organizace',
  featureOrganizationText: '\u0160koly, t\u0159\u00EDdy, role a organiza\u010Dn\u00ED struktura.',
  featureAcademicsTitle: 'Studium',
  featureAcademicsText: 'Rozvrh, doch\u00E1zka, zn\u00E1mky, \u00FAkoly a denn\u00ED agenda.',
  featureCommunicationTitle: 'Komunikace',
  featureCommunicationText: 'Ozn\u00E1men\u00ED a spojen\u00ED mezi \u0161kolou, u\u010Diteli a rodi\u010Di.',
  routeDashboard: 'P\u0159ehled',
  routeAcademics: 'Studium',
  routeIdentity: 'Profil',
  routeSecurity: 'Bezpečnost',
  'security.title': 'Zabezpečení',
  'security.overview': 'Přehled zabezpečení',
  'security.changePassword': 'Změna hesla',
  'security.changeEmail': 'Změna e-mailu',
  'security.mfa': 'Dvoufázové ověření',
  'security.recoveryCodes': 'Obnovovací kódy',
  'security.currentPassword': 'Aktuální heslo',
  'security.newPassword': 'Nové heslo',
  'security.confirmPassword': 'Potvrzení nového hesla',
  'security.currentEmail': 'Aktuální e-mail',
  'security.newEmail': 'Nová e-mailová adresa',
  'security.enableMfa': 'Zapnout dvoufázové ověření',
  'security.disableMfa': 'Vypnout dvoufázové ověření',
  'security.regenerateRecoveryCodes': 'Vygenerovat nové obnovovací kódy',
  securityTitle: 'Bezpečnost',
  securityDescription: 'Identity security self-service oddělený od business profilu.',
  securitySummaryTitle: 'Bezpečnostní souhrn',
  securityCurrentEmail: 'Aktuální e-mail',
  securityEmailConfirmed: 'E-mail potvrzen',
  securityMfaEnabled: 'MFA zapnuto',
  securityRecoveryCodesLeft: 'Zbývající recovery kódy',
  securityYes: 'Ano',
  securityNo: 'Ne',
  securityChangePasswordTitle: 'Změna hesla',
  securityCurrentPassword: 'Aktuální heslo',
  securityNewPassword: 'Nové heslo',
  securityConfirmNewPassword: 'Potvrzení nového hesla',
  securityChangePasswordAction: 'Změnit heslo',
  securityChangeEmailTitle: 'Změna e-mailu',
  securityCurrentPasswordReauth: 'Aktuální heslo (re-auth)',
  securityNewEmail: 'Nový e-mail',
  securityRequestEmailChangeAction: 'Vyžádat změnu e-mailu',
  securityMfaManagementTitle: 'MFA správa (TOTP + recovery kódy)',
  securityMfaEnabledBadge: 'MFA zapnuto',
  securityMfaDisabledBadge: 'MFA vypnuto',
  securityStartMfaSetupAction: 'Spustit MFA setup',
  securitySharedKey: 'Sdílený klíč',
  securityAuthenticatorUri: 'Authenticator URI',
  securityVerificationCode: 'Verifikační kód',
  securityConfirmMfaAction: 'Potvrdit MFA',
  securityMfaVerificationCode: 'MFA verifikační kód',
  securityDisableMfaAction: 'Vypnout MFA',
  securityRegenerateRecoveryCodesAction: 'Regenerovat recovery kódy',
  securityRecoveryCodesTitle: 'Recovery kódy',
  securityRecoveryCodesEmpty: 'Recovery kódy se zobrazí až po potvrzení setupu nebo regeneraci.',
  securityLoading: 'Načítám security self-service...',
  securitySummaryNotAvailable: 'Security souhrn není dostupný.',
  securityMfaEnabledSuccess: 'MFA zapnuto.',
  securityRecoveryRegeneratedSuccess: 'Recovery kódy byly regenerovány.',
  securityForgotPasswordTitle: 'Zapomenuté heslo',
  securityForgotPasswordDescription: 'Vyžádejte e-mail pro reset hesla. Odpověď je z bezpečnostních důvodů generická.',
  securityForgotPasswordAction: 'Odeslat reset e-mail',
  securityResetPasswordTitle: 'Reset hesla',
  securityResetPasswordDescription: 'Dokončete reset hesla pomocí bezpečného tokenu.',
  securityResetPasswordAction: 'Resetovat heslo',
  securityConfirmEmailChangeLoading: 'Potvrzuji změnu e-mailu...',
  securityMissingResetParams: 'Chybí parametry reset tokenu.',
  securityMissingEmailConfirmParams: 'Chybí parametry potvrzení e-mailu.',
  roleUser: 'U\u017Eivatel',
  dashboardSuffix: 'p\u0159ehled',
  dashboardKindergarten: 'Skupiny, denn\u00ED reporty, doch\u00E1zka a komunikace s rodi\u010Di.',
  dashboardSecondary: 'T\u0159\u00EDdy, p\u0159edm\u011Bty, obory a \u0161ir\u0161\u00ED \u0161koln\u00ED agenda.',
  dashboardDefault: 'T\u0159\u00EDdy, p\u0159edm\u011Bty, rozvrh, doch\u00E1zka, zn\u00E1mky a \u00FAkoly.',
  loadingOrganization: 'Na\u010D\u00EDt\u00E1m organiza\u010Dn\u00ED pohled...',
  academicsKindergartenHint: 'Workflow denn\u00EDch report\u016F je zv\u00FDrazn\u011Bn\u00FD.',
  academicsDefaultHint: 'Rozvrh, v\u00FDuka, doch\u00E1zka, zn\u00E1mky a \u00FAkoly.',
  loadDailyReports: 'Na\u010D\u00EDst denn\u00ED reporty',
  connectionState: 'P\u0159ipojen\u00ED',
  connected: 'p\u0159ipojeno',
  disconnected: 'odpojeno',
  administrationTitle: 'Administrace',
  systemSettings: 'Syst\u00E9mov\u00E1 nastaven\u00ED',
  unauthorizedAdministration: 'Nem\u00E1te opr\u00E1vn\u011Bn\u00ED pro administraci.',
  unauthorizedIdentity: 'Nem\u00E1te opr\u00E1vn\u011Bn\u00ED pro detaily profilu.',
  processingCallback: 'Zpracov\u00E1v\u00E1m p\u0159ihl\u00E1\u0161en\u00ED...',
  authCompleted: 'P\u0159ihl\u00E1\u0161en\u00ED dokon\u010Deno. P\u0159esm\u011Brov\u00E1v\u00E1m...',
  authFailed: 'P\u0159ihl\u00E1\u0161en\u00ED selhalo',
  missingAuthState: 'Chyb\u00ED autoriza\u010Dn\u00ED stav.',
  stateValidationFailed: 'Validace stavu selhala.',
  tokenExchangeFailed: 'V\u00FDm\u011Bna tokenu selhala se stavem {status}',
  shellSubtitle: 'Role-based a school-type-aware aplika\u010Dn\u00ED shell.',
  shellApplication: 'Aplika\u010Dn\u00ED shell',
  language: 'Jazyk',
  selectLanguage: 'Vyberte jazyk',
  notifications: 'Ozn\u00E1men\u00ED',
  mainNavigation: 'Hlavn\u00ED navigace',
  openProfileMenu: 'Otev\u0159\u00EDt profilov\u00E9 menu',
  close: 'Zav\u0159\u00EDt',
  menu: 'Menu',
  myProfile: 'M\u016Fj profil',
  'myProfile.title': 'M\u016Fj profil',
  'myProfile.accountOverview': 'P\u0159ehled \u00FA\u010Dtu',
  'myProfile.personalEdit': 'Vlastn\u00ED \u00FAprava profilu',
  'myProfile.roleAssignments': 'P\u0159i\u0159azen\u00E9 role',
  'myProfile.parentStudentLinks': 'Vazby rodi\u010D-student',
  profile: 'Profil',
  signOutMenu: 'Odhl\u00E1sit',
  overview: 'P\u0159ehled',
  operationsKindergarten: 'Operace (Skupiny a denn\u00ED reporty)',
  operationsElementary: 'Operace (T\u0159\u00EDdy a p\u0159edm\u011Bty)',
  operationsSecondary: 'Operace (T\u0159\u00EDdy, p\u0159edm\u011Bty, studijn\u00ED kontext)',
  navSchools: '\u0160koly',
  navSchoolYears: '\u0160koln\u00ED roky',
  navGradeLevels: 'Ro\u010Dn\u00EDky',
  navClasses: 'T\u0159\u00EDdy',
  navGroups: 'Skupiny',
  navSubjects: 'P\u0159edm\u011Bty',
  navFieldsOfStudy: 'Obory',
  navTeacherAssignments: 'P\u0159i\u0159azen\u00ED u\u010Ditel\u016F',
  navTimetable: 'Rozvrh',
  navLessonRecords: 'Z\u00E1znamy hodin',
  navAttendance: 'Doch\u00E1zka',
  navExcuses: 'Omluvenky',
  navGrades: 'Zn\u00E1mky',
  navHomework: '\u00DAkoly',
  navDailyReports: 'Denn\u00ED reporty',
  create: 'Vytvo\u0159it',
  save: 'Ulo\u017Eit',
  cancel: 'Zru\u0161it',
  reloadLabel: 'Na\u010D\u00EDst znovu',
  open: 'Otev\u0159\u00EDt',
  send: 'Odeslat',
  publish: 'Publikovat',
  activate: 'Aktivovat',
  deactivate: 'Deaktivovat',
  loading: 'Na\u010D\u00EDt\u00E1m...',
  noData: '\u017D\u00E1dn\u00E1 data v aktu\u00E1ln\u00EDm rozsahu.',
  noRecords: '\u017D\u00E1dn\u00E9 z\u00E1znamy v aktu\u00E1ln\u00EDm rozsahu.',
  stateActive: 'Aktivn\u00ED',
  stateInactive: 'Neaktivn\u00ED',
  stateRead: '\u010Cten\u00ED',
  profileHeaderTitle: 'Profil',
  profileHeaderDescription: 'Business profil self-service s p\u0159\u00EDsn\u00FDmi read-only hranicemi pro identity assignmenty a vazby.',
  profileLabelRole: 'Role',
  profileLabelSchoolContext: '\u0160koln\u00ED kontext',
  profileLabelAssignedSchools: 'P\u0159i\u0159azen\u00E9 \u0161koly',
  profileLabelClass: 'T\u0159\u00EDda',
  profileLabelGroup: 'Skupina',
  profileLabelSubject: 'P\u0159edm\u011Bt',
  profileLabelAccountActive: '\u00DA\u010Det aktivn\u00ED',
  profileValueYes: 'Ano',
  profileValueNo: 'Ne',
  profileSaveSuccess: 'Profil ulo\u017Een.',
  profileAdminSaveSuccess: 'Administrativn\u00ED \u00FAprava profilu ulo\u017Eena.',
  profileSaving: 'Ukl\u00E1d\u00E1m profil...',
  profileSaveErrorGeneric: 'Profil se nepoda\u0159ilo ulo\u017Eit. Zkuste to znovu.',
  profileSaveErrorValidation: 'Opravte zv\u00FDrazn\u011Bn\u00E1 pole.',
  profileSaveErrorInvalidSchoolPosition: 'Vybran\u00E1 pracovn\u00ED pozice nen\u00ED platn\u00E1 pro v\u00E1\u0161 \u0161koln\u00ED kontext.',
  profileFieldRequired: 'Toto pole je povinn\u00E9.',
  profileDismiss: 'Zav\u0159\u00EDt',
  profileLoading: 'Na\u010D\u00EDt\u00E1m profil...',
  profileNotAvailable: 'Profil nen\u00ED dostupn\u00FD.',
  profileSelfEditTitle: 'Vlastn\u00ED \u00FAprava profilu',
  profileReadOnlyHint: 'Email / u\u017Eivatelsk\u00E9 jm\u00E9no / p\u0159i\u0159azen\u00ED rol\u00ED jsou jen pro \u010Dten\u00ED',
  profileFieldFirstName: 'Jm\u00E9no',
  profileFieldLastName: 'P\u0159\u00EDjmen\u00ED',
  profileFieldPreferredDisplayName: 'Preferovan\u00E9 zobrazovan\u00E9 jm\u00E9no',
  profileFieldPreferredLanguage: 'Preferovan\u00FD jazyk',
  profileFieldPhoneNumber: 'Telefon',
  profileFieldPositionTitle: 'Pracovn\u00ED pozice',
  profileFieldSchoolPosition: 'Pracovn\u00ED pozice ve \u0161kole',
  profileSelectSchoolPositionPlaceholder: 'Vyberte pracovn\u00ED pozici ve \u0161kole',
  profileSchoolPositionLoading: 'Na\u010D\u00EDt\u00E1m \u0161koln\u00ED pracovn\u00ED pozice...',
  profileSchoolPositionUnavailable: 'Pro aktu\u00E1ln\u00ED \u0161koln\u00ED kontext nejsou dostupn\u00E9 \u017E\u00E1dn\u00E9 pracovn\u00ED pozice.',
  profileFieldPublicContactNote: 'Ve\u0159ejn\u00E1 kontaktn\u00ED pozn\u00E1mka',
  profileFieldPreferredContactNote: 'Preferovan\u00E1 kontaktn\u00ED pozn\u00E1mka',
  profileButtonSaveMyProfile: 'Ulo\u017Eit m\u016Fj profil',
  profileRoleAssignmentsTitle: 'P\u0159i\u0159azen\u00ED rol\u00ED',
  profileNoRoleAssignments: '\u017D\u00E1dn\u00E1 p\u0159i\u0159azen\u00ED rol\u00ED.',
  profileParentStudentLinksTitle: 'Vazby rodi\u010D-student',
  profileNoParentStudentLinks: '\u017D\u00E1dn\u00E9 vazby rodi\u010D-student.',
  profileTeacherAssignmentsTitle: 'P\u0159i\u0159azen\u00ED u\u010Ditele',
  profileNoTeacherAssignments: 'V aktu\u00E1ln\u00EDm \u0161koln\u00EDm kontextu nejsou \u017E\u00E1dn\u00E1 p\u0159i\u0159azen\u00ED u\u010Ditele.',
  profileLinkedStudentsTitle: 'P\u0159ehled nav\u00E1zan\u00FDch student\u016F',
  profileNoLinkedStudents: '\u017D\u00E1dn\u00ED nav\u00E1zan\u00ED studenti.',
  profileAdminEditTitle: 'Administrativn\u00ED \u00FAprava profilu',
  profileAdminEditDescription: 'P\u0159i\u0159azen\u00ED rol\u00ED z\u016Fst\u00E1v\u00E1 ve vyhrazen\u00E9 boundary pro spr\u00E1vu rol\u00ED a zde se needituje.',
  profileAdminUserSelectLabel: 'U\u017Eivatelsk\u00FD profil',
  profileAdminUserSelectPlaceholder: 'Vyberte u\u017Eivatele',
  profileButtonSaveAdminEdit: 'Ulo\u017Eit administrativn\u00ED \u00FApravu',
  profileSelectLanguagePlaceholder: 'Vyberte jazyk',
  validationPasswordConfirmationMismatch: 'Potvrzeni hesla se neshoduje.',
  securityInvalidEmailFormat: 'Neplatný formát e-mailu.',
  validationRequiredAttendanceRecordId: 'ID záznamu docházky je povinné.',
  validationRequiredReason: 'Důvod je povinný.',
  validationGeneric: 'Validace selhala.',
  errorForbidden: 'Nemate opravneni k pristupu na tuto stranku.',
  errorNotFound: 'Pozadovana stranka nebyla nalezena.',
  errorUnexpected: 'Nastala neocekavana chyba.'
};

const sk: Translations = {
  ...en,
  appTitle: 'Skolio aplik\u00E1cia',
  signIn: 'Prihl\u00E1si\u0165 sa',
  signOut: 'Odhl\u00E1si\u0165 sa',
  goToLogin: 'Prejs\u0165 na prihl\u00E1senie',
  language: 'Jazyk',
  selectLanguage: 'Vyberte jazyk',
  notifications: 'Notifik\u00E1cie',
  myProfile: 'M\u00F4j profil',
  'myProfile.title': 'M\u00F4j profil',
  'myProfile.accountOverview': 'Preh\u013Ead \u00FA\u010Dtu',
  'myProfile.personalEdit': 'Vlastn\u00E1 \u00FAprava profilu',
  'myProfile.roleAssignments': 'Priraden\u00E9 roly',
  'myProfile.parentStudentLinks': 'Väzby rodi\u010D-\u0161tudent',
  routeSecurity: 'Bezpecnost',
  'security.title': 'Zabezpe\u010Denie',
  'security.overview': 'Preh\u013Ead zabezpe\u010Denia',
  'security.changePassword': 'Zmena hesla',
  'security.changeEmail': 'Zmena e-mailu',
  'security.mfa': 'Dvojfázové overenie',
  'security.recoveryCodes': 'Obnovovacie kódy',
  'security.currentPassword': 'Aktuálne heslo',
  'security.newPassword': 'Nové heslo',
  'security.confirmPassword': 'Potvrdenie nového hesla',
  'security.currentEmail': 'Aktuálny e-mail',
  'security.newEmail': 'Nová e-mailová adresa',
  'security.enableMfa': 'Zapnúť dvojfázové overenie',
  'security.disableMfa': 'Vypnúť dvojfázové overenie',
  'security.regenerateRecoveryCodes': 'Vygenerovať nové obnovovacie kódy',
  securityTitle: 'Bezpecnost',
  securityDescription: 'Identity security self-service oddeleny od business profilu.',
  securitySummaryTitle: 'Bezpecnostny suhrn',
  securityCurrentEmail: 'Aktualny e-mail',
  securityEmailConfirmed: 'E-mail potvrdeny',
  securityMfaEnabled: 'MFA zapnute',
  securityRecoveryCodesLeft: 'Zostavajuce recovery kody',
  securityYes: 'Ano',
  securityNo: 'Nie',
  securityChangePasswordTitle: 'Zmena hesla',
  securityCurrentPassword: 'Aktualne heslo',
  securityNewPassword: 'Nove heslo',
  securityConfirmNewPassword: 'Potvrdenie noveho hesla',
  securityChangePasswordAction: 'Zmenit heslo',
  securityChangeEmailTitle: 'Zmena e-mailu',
  securityCurrentPasswordReauth: 'Aktualne heslo (re-auth)',
  securityNewEmail: 'Novy e-mail',
  securityRequestEmailChangeAction: 'Vyziadat zmenu e-mailu',
  securityMfaManagementTitle: 'MFA sprava (TOTP + recovery kody)',
  securityMfaEnabledBadge: 'MFA zapnute',
  securityMfaDisabledBadge: 'MFA vypnute',
  securityStartMfaSetupAction: 'Spustit MFA setup',
  securitySharedKey: 'Zdielany kluc',
  securityAuthenticatorUri: 'Authenticator URI',
  securityVerificationCode: 'Verifikacny kod',
  securityConfirmMfaAction: 'Potvrdit MFA',
  securityMfaVerificationCode: 'MFA verifikacny kod',
  securityDisableMfaAction: 'Vypnut MFA',
  securityRegenerateRecoveryCodesAction: 'Regenerovat recovery kody',
  securityRecoveryCodesTitle: 'Recovery kody',
  securityRecoveryCodesEmpty: 'Recovery kody sa zobrazia az po potvrdeni setupu alebo regeneracii.',
  securityLoading: 'Nacitavam security self-service...',
  securitySummaryNotAvailable: 'Security suhrn nie je dostupny.',
  securityMfaEnabledSuccess: 'MFA zapnute.',
  securityRecoveryRegeneratedSuccess: 'Recovery kody boli regenerovane.',
  securityForgotPasswordTitle: 'Zabudnute heslo',
  securityForgotPasswordDescription: 'Vyziadajte e-mail pre reset hesla. Odpoved je z bezpecnostnych dovodov genericka.',
  securityForgotPasswordAction: 'Odoslat reset e-mail',
  securityResetPasswordTitle: 'Reset hesla',
  securityResetPasswordDescription: 'Dokoncite reset hesla pomocou bezpecneho tokenu.',
  securityResetPasswordAction: 'Resetovat heslo',
  securityConfirmEmailChangeLoading: 'Potvrdzujem zmenu e-mailu...',
  securityMissingResetParams: 'Chybaju parametre reset tokenu.',
  securityMissingEmailConfirmParams: 'Chybaju parametre potvrdenia e-mailu.',
  profile: 'Profil',
  signOutMenu: 'Odhl\u00E1si\u0165',
  overview: 'Preh\u013Ead',
  navSchools: '\u0160koly',
  navSchoolYears: '\u0160kolsk\u00E9 roky',
  navGradeLevels: 'Ro\u010Dn\u00EDky',
  navClasses: 'Triedy',
  navGroups: 'Skupiny',
  navSubjects: 'Predmety',
  navFieldsOfStudy: 'Odbory',
  navTeacherAssignments: 'Priradenia u\u010Dite\u013Eov',
  navTimetable: 'Rozvrh',
  navLessonRecords: 'Z\u00E1znamy hod\u00EDn',
  navAttendance: 'Doch\u00E1dzka',
  navExcuses: 'Ospravedlnenky',
  navGrades: 'Zn\u00E1mky',
  navHomework: '\u00DAlohy',
  navDailyReports: 'Denn\u00E9 reporty',
  create: 'Vytvori\u0165',
  save: 'Ulo\u017Ei\u0165',
  cancel: 'Zru\u0161i\u0165',
  reloadLabel: 'Na\u010D\u00EDta\u0165 znova',
  open: 'Otvori\u0165',
  send: 'Odosla\u0165',
  publish: 'Publikova\u0165',
  activate: 'Aktivova\u0165',
  deactivate: 'Deaktivova\u0165',
  loading: 'Na\u010D\u00EDtavam...',
  noData: '\u017Diadne d\u00E1ta v aktu\u00E1lnom rozsahu.',
  noRecords: '\u017Diadne z\u00E1znamy v aktu\u00E1lnom rozsahu.',
  stateActive: 'Akt\u00EDvne',
  stateInactive: 'Neakt\u00EDvne',
  stateRead: '\u010C\u00EDtanie',
  profileHeaderTitle: 'Profil',
  profileHeaderDescription: 'Business profil self-service s pr\u00EDsnymi hranicami iba na \u010D\u00EDtanie pre identity assignmenty a väzby.',
  profileLabelRole: 'Rola',
  profileLabelSchoolContext: '\u0160kolsk\u00FD kontext',
  profileLabelAssignedSchools: 'Priraden\u00E9 \u0161koly',
  profileLabelClass: 'Trieda',
  profileLabelGroup: 'Skupina',
  profileLabelSubject: 'Predmet',
  profileLabelAccountActive: '\u00DA\u010Det akt\u00EDvny',
  profileValueYes: '\u00C1no',
  profileValueNo: 'Nie',
  profileSaveSuccess: 'Profil bol ulo\u017Een\u00FD.',
  profileAdminSaveSuccess: 'Administrat\u00EDvna \u00FAprava profilu bola ulo\u017Een\u00E1.',
  profileSaving: 'Uklad\u00E1m profil...',
  profileSaveErrorGeneric: 'Profil sa nepodarilo ulo\u017Ei\u0165. Sk\u00FAste to znova.',
  profileSaveErrorValidation: 'Opravte zv\u00FDraznen\u00E9 polia.',
  profileSaveErrorInvalidSchoolPosition: 'Vybran\u00E1 pracovn\u00E1 poz\u00EDcia nie je platn\u00E1 pre v\u00E1\u0161 \u0161kolsk\u00FD kontext.',
  profileFieldRequired: 'Toto pole je povinn\u00E9.',
  profileDismiss: 'Zavrie\u0165',
  profileLoading: 'Na\u010D\u00EDtavam profil...',
  profileNotAvailable: 'Profil nie je dostupn\u00FD.',
  profileSelfEditTitle: 'Vlastn\u00E1 \u00FAprava profilu',
  profileReadOnlyHint: 'Email / pou\u017E\u00EDvate\u013Esk\u00E9 meno / priradenia rol\u00ED s\u00FA iba na \u010D\u00EDtanie',
  profileFieldFirstName: 'Meno',
  profileFieldLastName: 'Priezvisko',
  profileFieldPreferredDisplayName: 'Preferovan\u00E9 zobrazovan\u00E9 meno',
  profileFieldPreferredLanguage: 'Preferovan\u00FD jazyk',
  profileFieldPhoneNumber: 'Telef\u00F3n',
  profileFieldPositionTitle: 'Pracovn\u00E1 poz\u00EDcia',
  profileFieldSchoolPosition: 'Pracovn\u00E1 poz\u00EDcia v \u0161kole',
  profileSelectSchoolPositionPlaceholder: 'Vyberte pracovn\u00FA poz\u00EDciu v \u0161kole',
  profileSchoolPositionLoading: 'Na\u010D\u00EDtavam \u0161kolsk\u00E9 pracovn\u00E9 poz\u00EDcie...',
  profileSchoolPositionUnavailable: 'Pre aktu\u00E1lny \u0161kolsk\u00FD kontext nie s\u00FA dostupn\u00E9 \u017Eiadne pracovn\u00E9 poz\u00EDcie.',
  profileFieldPublicContactNote: 'Verejn\u00E1 kontaktn\u00E1 pozn\u00E1mka',
  profileFieldPreferredContactNote: 'Preferovan\u00E1 kontaktn\u00E1 pozn\u00E1mka',
  profileButtonSaveMyProfile: 'Ulo\u017Ei\u0165 m\u00F4j profil',
  profileRoleAssignmentsTitle: 'Priradenia rol\u00ED',
  profileNoRoleAssignments: '\u017Diadne priradenia rol\u00ED.',
  profileParentStudentLinksTitle: 'Väzby rodi\u010D-\u0161tudent',
  profileNoParentStudentLinks: '\u017Diadne väzby rodi\u010D-\u0161tudent.',
  profileTeacherAssignmentsTitle: 'Priradenia u\u010Dite\u013Ea',
  profileNoTeacherAssignments: 'V aktu\u00E1lnom \u0161kolskom kontexte nie s\u00FA \u017Eiadne priradenia u\u010Dite\u013Ea.',
  profileLinkedStudentsTitle: 'Preh\u013Ead prepojen\u00FDch \u0161tudentov',
  profileNoLinkedStudents: '\u017Diadni prepojen\u00ED \u0161tudenti.',
  profileAdminEditTitle: 'Administrat\u00EDvna \u00FAprava profilu',
  profileAdminEditDescription: 'Priradenia rol\u00ED zost\u00E1vaj\u00FA v samostatnej boundary pre spr\u00E1vu rol\u00ED a tu sa needituj\u00FA.',
  profileAdminUserSelectLabel: 'Pou\u017E\u00EDvate\u013Esk\u00FD profil',
  profileAdminUserSelectPlaceholder: 'Vyberte pou\u017E\u00EDvate\u013Ea',
  profileButtonSaveAdminEdit: 'Ulo\u017Ei\u0165 administrat\u00EDvnu \u00FApravu',
  profileSelectLanguagePlaceholder: 'Vyberte jazyk',
  validationPasswordConfirmationMismatch: 'Potvrdenie hesla sa nezhoduje.',
  securityInvalidEmailFormat: 'Neplatný formát e-mailu.',
  validationRequiredAttendanceRecordId: 'ID záznamu dochádzky je povinné.',
  validationRequiredReason: 'Dôvod je povinný.',
  validationGeneric: 'Validácia zlyhala.',
  errorForbidden: 'Nemate opravnenie na pristup na tuto stranku.',
  errorNotFound: 'Pozadovana stranka nebola najdena.',
  errorUnexpected: 'Nastala necakana chyba.'
};

const de: Translations = {
  ...en,
  signIn: 'Anmelden',
  signOut: 'Abmelden',
  goToLogin: 'Zur Anmeldung',
  language: 'Sprache',
  selectLanguage: 'Sprache ausw\u00E4hlen',
  notifications: 'Benachrichtigungen',
  myProfile: 'Mein Profil',
  'myProfile.title': 'Mein Profil',
  'myProfile.accountOverview': 'Kontenübersicht',
  'myProfile.personalEdit': 'Eigene Profilbearbeitung',
  'myProfile.roleAssignments': 'Zugewiesene Rollen',
  'myProfile.parentStudentLinks': 'Eltern-Schüler-Verknüpfungen',
  routeSecurity: 'Sicherheit',
  'security.title': 'Sicherheit',
  'security.overview': 'Sicherheitsübersicht',
  'security.changePassword': 'Passwort ändern',
  'security.changeEmail': 'E-Mail ändern',
  'security.mfa': 'Zwei-Faktor-Authentifizierung',
  'security.recoveryCodes': 'Wiederherstellungscodes',
  'security.currentPassword': 'Aktuelles Passwort',
  'security.newPassword': 'Neues Passwort',
  'security.confirmPassword': 'Neues Passwort bestätigen',
  'security.currentEmail': 'Aktuelle E-Mail',
  'security.newEmail': 'Neue E-Mail-Adresse',
  'security.enableMfa': 'Zwei-Faktor-Authentifizierung aktivieren',
  'security.disableMfa': 'Zwei-Faktor-Authentifizierung deaktivieren',
  'security.regenerateRecoveryCodes': 'Neue Wiederherstellungscodes generieren',
  securityTitle: 'Sicherheit',
  securityDescription: 'Identity-Sicherheits-Self-Service getrennt vom Business-Profil.',
  securitySummaryTitle: 'Sicherheitsübersicht',
  securityCurrentEmail: 'Aktuelle E-Mail',
  securityEmailConfirmed: 'E-Mail bestätigt',
  securityMfaEnabled: 'MFA aktiviert',
  securityRecoveryCodesLeft: 'Verbleibende Recovery-Codes',
  securityYes: 'Ja',
  securityNo: 'Nein',
  securityChangePasswordTitle: 'Passwort ändern',
  securityCurrentPassword: 'Aktuelles Passwort',
  securityNewPassword: 'Neues Passwort',
  securityConfirmNewPassword: 'Neues Passwort bestätigen',
  securityChangePasswordAction: 'Passwort ändern',
  securityChangeEmailTitle: 'E-Mail ändern',
  securityCurrentPasswordReauth: 'Aktuelles Passwort (Re-Auth)',
  securityNewEmail: 'Neue E-Mail',
  securityRequestEmailChangeAction: 'E-Mail-Änderung anfordern',
  securityMfaManagementTitle: 'MFA-Verwaltung (TOTP + Recovery-Codes)',
  securityMfaEnabledBadge: 'MFA aktiviert',
  securityMfaDisabledBadge: 'MFA deaktiviert',
  securityStartMfaSetupAction: 'MFA-Setup starten',
  securitySharedKey: 'Gemeinsamer Schlüssel',
  securityAuthenticatorUri: 'Authenticator-URI',
  securityVerificationCode: 'Verifizierungscode',
  securityConfirmMfaAction: 'MFA bestätigen',
  securityMfaVerificationCode: 'MFA-Verifizierungscode',
  securityDisableMfaAction: 'MFA deaktivieren',
  securityRegenerateRecoveryCodesAction: 'Recovery-Codes neu generieren',
  securityRecoveryCodesTitle: 'Recovery-Codes',
  securityRecoveryCodesEmpty: 'Recovery-Codes werden nur nach Setup-Bestätigung oder Regeneration angezeigt.',
  securityLoading: 'Sicherheits-Self-Service wird geladen...',
  securitySummaryNotAvailable: 'Sicherheitsübersicht ist nicht verfügbar.',
  securityMfaEnabledSuccess: 'MFA aktiviert.',
  securityRecoveryRegeneratedSuccess: 'Recovery-Codes wurden neu generiert.',
  securityForgotPasswordTitle: 'Passwort vergessen',
  securityForgotPasswordDescription: 'Passwort-Reset-E-Mail anfordern. Die Antwort ist aus Sicherheitsgründen generisch.',
  securityForgotPasswordAction: 'Reset-E-Mail senden',
  securityResetPasswordTitle: 'Passwort zurücksetzen',
  securityResetPasswordDescription: 'Passwort-Reset mit sicherem Token abschließen.',
  securityResetPasswordAction: 'Passwort zurücksetzen',
  securityConfirmEmailChangeLoading: 'E-Mail-Änderung wird bestätigt...',
  securityMissingResetParams: 'Parameter für Reset-Token fehlen.',
  securityMissingEmailConfirmParams: 'Parameter für E-Mail-Bestätigung fehlen.',
  profile: 'Profil',
  signOutMenu: 'Abmelden',
  overview: '\u00DCbersicht',
  navSchools: 'Schulen',
  navSchoolYears: 'Schuljahre',
  navGradeLevels: 'Jahrg\u00E4nge',
  navClasses: 'Klassen',
  navGroups: 'Gruppen',
  navSubjects: 'F\u00E4cher',
  navFieldsOfStudy: 'Fachrichtungen',
  navTeacherAssignments: 'Lehrerzuweisungen',
  navTimetable: 'Stundenplan',
  navLessonRecords: 'Unterrichtsprotokolle',
  navAttendance: 'Anwesenheit',
  navExcuses: 'Entschuldigungen',
  navGrades: 'Noten',
  navHomework: 'Hausaufgaben',
  navDailyReports: 'Tagesberichte',
  create: 'Erstellen',
  save: 'Speichern',
  cancel: 'Abbrechen',
  reloadLabel: 'Neu laden',
  open: '\u00D6ffnen',
  send: 'Senden',
  publish: 'Ver\u00F6ffentlichen',
  activate: 'Aktivieren',
  deactivate: 'Deaktivieren',
  loading: 'Lade...',
  noData: 'Keine Daten im aktuellen Bereich.',
  noRecords: 'Keine Eintr\u00E4ge im aktuellen Bereich.',
  stateActive: 'Aktiv',
  stateInactive: 'Inaktiv',
  stateRead: 'Lesen',
  profileHeaderTitle: 'Profil',
  profileHeaderDescription: 'Business-Profil Self-Service mit strikten Nur-Lesen-Grenzen für Identity-Zuweisungen und Verknüpfungen.',
  profileLabelRole: 'Rolle',
  profileLabelSchoolContext: 'Schulkontext',
  profileLabelAssignedSchools: 'Zugewiesene Schulen',
  profileLabelClass: 'Klasse',
  profileLabelGroup: 'Gruppe',
  profileLabelSubject: 'Fach',
  profileLabelAccountActive: 'Konto aktiv',
  profileValueYes: 'Ja',
  profileValueNo: 'Nein',
  profileSaveSuccess: 'Profil wurde gespeichert.',
  profileAdminSaveSuccess: 'Administrative Profiländerung wurde gespeichert.',
  profileSaving: 'Profil wird gespeichert...',
  profileSaveErrorGeneric: 'Profil konnte nicht gespeichert werden. Bitte erneut versuchen.',
  profileSaveErrorValidation: 'Bitte korrigieren Sie die markierten Felder.',
  profileSaveErrorInvalidSchoolPosition: 'Die ausgewählte Schulposition ist für Ihren Schulkontext nicht gültig.',
  profileFieldRequired: 'Dieses Feld ist erforderlich.',
  profileDismiss: 'Schließen',
  profileLoading: 'Profil wird geladen...',
  profileNotAvailable: 'Profil ist nicht verfügbar.',
  profileSelfEditTitle: 'Eigene Profilbearbeitung',
  profileReadOnlyHint: 'E-Mail / Benutzername / Rollenzuweisungen sind schreibgeschützt',
  profileFieldFirstName: 'Vorname',
  profileFieldLastName: 'Nachname',
  profileFieldPreferredDisplayName: 'Bevorzugter Anzeigename',
  profileFieldPreferredLanguage: 'Bevorzugte Sprache',
  profileFieldPhoneNumber: 'Telefonnummer',
  profileFieldPositionTitle: 'Positionsbezeichnung',
  profileFieldSchoolPosition: 'Schulposition',
  profileSelectSchoolPositionPlaceholder: 'Schulposition ausw\u00E4hlen',
  profileSchoolPositionLoading: 'Schulpositionen werden geladen...',
  profileSchoolPositionUnavailable: 'F\u00FCr den aktuellen Schulkontext sind keine Schulpositionen verf\u00FCgbar.',
  profileFieldPublicContactNote: 'Öffentliche Kontaktnotiz',
  profileFieldPreferredContactNote: 'Bevorzugte Kontaktnotiz',
  profileButtonSaveMyProfile: 'Mein Profil speichern',
  profileRoleAssignmentsTitle: 'Rollenzuweisungen',
  profileNoRoleAssignments: 'Keine Rollenzuweisungen.',
  profileParentStudentLinksTitle: 'Eltern-Schüler-Verknüpfungen',
  profileNoParentStudentLinks: 'Keine Eltern-Schüler-Verknüpfungen.',
  profileTeacherAssignmentsTitle: 'Lehrerzuweisungen',
  profileNoTeacherAssignments: 'Keine Lehrerzuweisungen im gewählten Schulkontext.',
  profileLinkedStudentsTitle: 'Übersicht verknüpfter Schüler',
  profileNoLinkedStudents: 'Keine verknüpften Schüler.',
  profileAdminEditTitle: 'Administrative Profilbearbeitung',
  profileAdminEditDescription: 'Rollenzuweisungen bleiben in der dedizierten Rollenverwaltung und werden hier nicht bearbeitet.',
  profileAdminUserSelectLabel: 'Benutzerprofil',
  profileAdminUserSelectPlaceholder: 'Benutzer auswählen',
  profileButtonSaveAdminEdit: 'Administrative Änderung speichern',
  profileSelectLanguagePlaceholder: 'Sprache auswählen',
  validationPasswordConfirmationMismatch: 'Passwortbestätigung stimmt nicht überein.',
  securityInvalidEmailFormat: 'Ungültiges E-Mail-Format.',
  validationRequiredAttendanceRecordId: 'Die Anwesenheitsdatensatz-ID ist erforderlich.',
  validationRequiredReason: 'Begründung ist erforderlich.',
  validationGeneric: 'Validierung fehlgeschlagen.',
  errorForbidden: 'Sie sind für diese Seite nicht berechtigt.',
  errorNotFound: 'Die angeforderte Seite wurde nicht gefunden.',
  errorUnexpected: 'Es ist ein unerwarteter Fehler aufgetreten.'
};

const pl: Translations = {
  ...en,
  signIn: 'Zaloguj si\u0119',
  signOut: 'Wyloguj si\u0119',
  goToLogin: 'Przejd\u017A do logowania',
  language: 'J\u0119zyk',
  selectLanguage: 'Wybierz j\u0119zyk',
  notifications: 'Powiadomienia',
  myProfile: 'M\u00F3j profil',
  'myProfile.title': 'M\u00F3j profil',
  'myProfile.accountOverview': 'Przegl\u0105d konta',
  'myProfile.personalEdit': 'Edycja w\u0142asnego profilu',
  'myProfile.roleAssignments': 'Przypisane role',
  'myProfile.parentStudentLinks': 'Powi\u0105zania rodzic-ucze\u0144',
  routeSecurity: 'Bezpieczenstwo',
  'security.title': 'Bezpieczeństwo',
  'security.overview': 'Przegląd bezpieczeństwa',
  'security.changePassword': 'Zmiana hasła',
  'security.changeEmail': 'Zmiana e-maila',
  'security.mfa': 'Weryfikacja dwuetapowa',
  'security.recoveryCodes': 'Kody odzyskiwania',
  'security.currentPassword': 'Aktualne hasło',
  'security.newPassword': 'Nowe hasło',
  'security.confirmPassword': 'Potwierdzenie nowego hasła',
  'security.currentEmail': 'Aktualny e-mail',
  'security.newEmail': 'Nowy adres e-mail',
  'security.enableMfa': 'Włącz weryfikację dwuetapową',
  'security.disableMfa': 'Wyłącz weryfikację dwuetapową',
  'security.regenerateRecoveryCodes': 'Wygeneruj nowe kody odzyskiwania',
  securityTitle: 'Bezpieczenstwo',
  securityDescription: 'Identity security self-service oddzielony od profilu biznesowego.',
  securitySummaryTitle: 'Podsumowanie bezpieczenstwa',
  securityCurrentEmail: 'Aktualny e-mail',
  securityEmailConfirmed: 'E-mail potwierdzony',
  securityMfaEnabled: 'MFA wlaczone',
  securityRecoveryCodesLeft: 'Pozostale kody recovery',
  securityYes: 'Tak',
  securityNo: 'Nie',
  securityChangePasswordTitle: 'Zmiana hasla',
  securityCurrentPassword: 'Aktualne haslo',
  securityNewPassword: 'Nowe haslo',
  securityConfirmNewPassword: 'Potwierdzenie nowego hasla',
  securityChangePasswordAction: 'Zmien haslo',
  securityChangeEmailTitle: 'Zmiana e-maila',
  securityCurrentPasswordReauth: 'Aktualne haslo (re-auth)',
  securityNewEmail: 'Nowy e-mail',
  securityRequestEmailChangeAction: 'Zadaj zmiany e-maila',
  securityMfaManagementTitle: 'Zarzadzanie MFA (TOTP + kody recovery)',
  securityMfaEnabledBadge: 'MFA wlaczone',
  securityMfaDisabledBadge: 'MFA wylaczone',
  securityStartMfaSetupAction: 'Rozpocznij konfiguracje MFA',
  securitySharedKey: 'Wspolny klucz',
  securityAuthenticatorUri: 'Authenticator URI',
  securityVerificationCode: 'Kod weryfikacyjny',
  securityConfirmMfaAction: 'Potwierdz MFA',
  securityMfaVerificationCode: 'Kod weryfikacyjny MFA',
  securityDisableMfaAction: 'Wylacz MFA',
  securityRegenerateRecoveryCodesAction: 'Wygeneruj ponownie kody recovery',
  securityRecoveryCodesTitle: 'Kody recovery',
  securityRecoveryCodesEmpty: 'Kody recovery sa widoczne dopiero po potwierdzeniu konfiguracji lub regeneracji.',
  securityLoading: 'Ladowanie security self-service...',
  securitySummaryNotAvailable: 'Podsumowanie security jest niedostepne.',
  securityMfaEnabledSuccess: 'MFA wlaczone.',
  securityRecoveryRegeneratedSuccess: 'Kody recovery zostaly zregenerowane.',
  securityForgotPasswordTitle: 'Zapomniane haslo',
  securityForgotPasswordDescription: 'Wyslij prosbe o e-mail resetu hasla. Odpowiedz jest celowo generyczna.',
  securityForgotPasswordAction: 'Wyslij e-mail resetu',
  securityResetPasswordTitle: 'Reset hasla',
  securityResetPasswordDescription: 'Zakoncz reset hasla przy uzyciu bezpiecznego tokenu.',
  securityResetPasswordAction: 'Zresetuj haslo',
  securityConfirmEmailChangeLoading: 'Potwierdzanie zmiany e-maila...',
  securityMissingResetParams: 'Brakuje parametrow tokenu resetu.',
  securityMissingEmailConfirmParams: 'Brakuje parametrow potwierdzenia e-maila.',
  profile: 'Profil',
  signOutMenu: 'Wyloguj',
  overview: 'Przegl\u0105d',
  navSchools: 'Szko\u0142y',
  navSchoolYears: 'Lata szkolne',
  navGradeLevels: 'Poziomy klas',
  navClasses: 'Klasy',
  navGroups: 'Grupy',
  navSubjects: 'Przedmioty',
  navFieldsOfStudy: 'Kierunki',
  navTeacherAssignments: 'Przypisania nauczycieli',
  navTimetable: 'Plan lekcji',
  navLessonRecords: 'Zapisy lekcji',
  navAttendance: 'Frekwencja',
  navExcuses: 'Usprawiedliwienia',
  navGrades: 'Oceny',
  navHomework: 'Prace domowe',
  navDailyReports: 'Raporty dzienne',
  create: 'Utw\u00F3rz',
  save: 'Zapisz',
  cancel: 'Anuluj',
  reloadLabel: 'Od\u015Bwie\u017C',
  open: 'Otw\u00F3rz',
  send: 'Wy\u015Blij',
  publish: 'Opublikuj',
  activate: 'Aktywuj',
  deactivate: 'Dezaktywuj',
  loading: '\u0141adowanie...',
  noData: 'Brak danych w bie\u017C\u0105cym zakresie.',
  noRecords: 'Brak rekord\u00F3w w bie\u017C\u0105cym zakresie.',
  stateActive: 'Aktywny',
  stateInactive: 'Nieaktywny',
  stateRead: 'Odczyt',
  profileHeaderTitle: 'Profil',
  profileHeaderDescription: 'Business self-service profilu z rygorystycznymi granicami tylko do odczytu dla przypisań tożsamości i powiązań.',
  profileLabelRole: 'Rola',
  profileLabelSchoolContext: 'Kontekst szkoły',
  profileLabelAssignedSchools: 'Przypisane szkoły',
  profileLabelClass: 'Klasa',
  profileLabelGroup: 'Grupa',
  profileLabelSubject: 'Przedmiot',
  profileLabelAccountActive: 'Konto aktywne',
  profileValueYes: 'Tak',
  profileValueNo: 'Nie',
  profileSaveSuccess: 'Profil został zapisany.',
  profileAdminSaveSuccess: 'Administracyjna edycja profilu została zapisana.',
  profileSaving: 'Zapisywanie profilu...',
  profileSaveErrorGeneric: 'Nie udało się zapisać profilu. Spróbuj ponownie.',
  profileSaveErrorValidation: 'Popraw wyróżnione pola.',
  profileSaveErrorInvalidSchoolPosition: 'Wybrane stanowisko w szkole jest nieprawidłowe dla bieżącego kontekstu szkoły.',
  profileFieldRequired: 'To pole jest wymagane.',
  profileDismiss: 'Zamknij',
  profileLoading: 'Ładowanie profilu...',
  profileNotAvailable: 'Profil jest niedostępny.',
  profileSelfEditTitle: 'Edycja własnego profilu',
  profileReadOnlyHint: 'Email / nazwa użytkownika / przypisania ról są tylko do odczytu',
  profileFieldFirstName: 'Imię',
  profileFieldLastName: 'Nazwisko',
  profileFieldPreferredDisplayName: 'Preferowana nazwa wyświetlana',
  profileFieldPreferredLanguage: 'Preferowany język',
  profileFieldPhoneNumber: 'Telefon',
  profileFieldPositionTitle: 'Stanowisko',
  profileFieldSchoolPosition: 'Stanowisko w szkole',
  profileSelectSchoolPositionPlaceholder: 'Wybierz stanowisko w szkole',
  profileSchoolPositionLoading: 'Ładowanie stanowisk szkolnych...',
  profileSchoolPositionUnavailable: 'Brak dostępnych stanowisk szkolnych dla bieżącego kontekstu szkoły.',
  profileFieldPublicContactNote: 'Publiczna notatka kontaktowa',
  profileFieldPreferredContactNote: 'Preferowana notatka kontaktowa',
  profileButtonSaveMyProfile: 'Zapisz mój profil',
  profileRoleAssignmentsTitle: 'Przypisania ról',
  profileNoRoleAssignments: 'Brak przypisań ról.',
  profileParentStudentLinksTitle: 'Powiązania rodzic-uczeń',
  profileNoParentStudentLinks: 'Brak powiązań rodzic-uczeń.',
  profileTeacherAssignmentsTitle: 'Przypisania nauczyciela',
  profileNoTeacherAssignments: 'Brak przypisań nauczyciela w wybranym kontekście szkoły.',
  profileLinkedStudentsTitle: 'Podsumowanie powiązanych uczniów',
  profileNoLinkedStudents: 'Brak powiązanych uczniów.',
  profileAdminEditTitle: 'Administracyjna edycja profilu',
  profileAdminEditDescription: 'Przypisania ról pozostają w dedykowanej granicy zarządzania rolami i nie są tu edytowane.',
  profileAdminUserSelectLabel: 'Profil użytkownika',
  profileAdminUserSelectPlaceholder: 'Wybierz użytkownika',
  profileButtonSaveAdminEdit: 'Zapisz edycję administracyjną',
  profileSelectLanguagePlaceholder: 'Wybierz język',
  validationPasswordConfirmationMismatch: 'Potwierdzenie hasla nie jest zgodne.',
  securityInvalidEmailFormat: 'Nieprawidłowy format adresu e-mail.',
  validationRequiredAttendanceRecordId: 'Identyfikator rekordu frekwencji jest wymagany.',
  validationRequiredReason: 'Powód jest wymagany.',
  validationGeneric: 'Walidacja nie powiodła się.',
  errorForbidden: 'Nie masz uprawnien do tej strony.',
  errorNotFound: 'Zadana strona nie zostala znaleziona.',
  errorUnexpected: 'Wystapil nieoczekiwany blad.'
};

const translations: Record<Locale, Translations> = {
  en,
  cs,
  sk,
  de,
  pl
};

type I18nContextValue = {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: TranslationKey, params?: Record<string, string | number>) => string;
};

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(() => {
    const stored = localStorage.getItem(STORAGE_KEY) as Locale | null;
    if (stored && stored in localeLabelsInternal) {
      return stored;
    }
    return 'cs';
  });

  const value = useMemo<I18nContextValue>(() => {
    const t = (key: TranslationKey, params?: Record<string, string | number>) => {
      let text = translations[locale][key] ?? translations.cs[key] ?? key;
      if (params) {
        for (const [paramKey, paramValue] of Object.entries(params)) {
          text = text.replaceAll(`{${paramKey}}`, String(paramValue));
        }
      }

      return text;
    };

    return {
      locale,
      setLocale: (nextLocale: Locale) => {
        localStorage.setItem(STORAGE_KEY, nextLocale);
        setLocaleState(nextLocale);
      },
      t
    };
  }, [locale]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error('useI18n must be used within I18nProvider');
  }

  return context;
}

export const supportedLocales = Object.keys(localeLabelsInternal) as Locale[];
export const localeLabels = localeLabelsInternal;
` 


