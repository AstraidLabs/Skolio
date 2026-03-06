using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Skolio.Administration.Api.Auth;
using Skolio.Administration.Api.Configuration;
using Skolio.Administration.Application;
using Skolio.Administration.Infrastructure;
using Skolio.Administration.Infrastructure.Extensions;
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
    options.AddPolicy(SkolioPolicies.SchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));
});
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddHealthChecks();builder.Services.AddOpenApi();builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));builder.Services.AddHangfireServer();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyAdministrationMigrationsAsync(); app.MapOpenApi(); }
app.UseCors("SkolioDevelopment");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.MapHealthChecks("/health");app.MapHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<HousekeepingJob>("administration-housekeeping-boundary", job => job.ExecuteAsync(), Cron.HourInterval(6));
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AdministrationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-5-auth-ready", publicBaseUrl = options.Value.PublicBaseUrl, hangfireDashboard = "/hangfire" }));
app.Run();
