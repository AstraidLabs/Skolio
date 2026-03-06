using System.Security.Claims;
using OpenIddict.Abstractions;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Application;
using Skolio.Identity.Infrastructure;
using Skolio.Identity.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<IdentityServiceOptions>().Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SkolioPolicies.PlatformAdministration, policy => policy.RequireRole("PlatformAdministrator"));
    options.AddPolicy(SkolioPolicies.SchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));
});

var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyIdentityMigrationsAsync(); app.MapOpenApi(); }

app.UseCors("SkolioDevelopment");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<IdentityServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-5-auth-ready", publicBaseUrl = options.Value.PublicBaseUrl, authWiring = "openiddict-authorization-server" }));
app.MapGet("/.well-known/jwks.json", () => Results.Redirect("/.well-known/openid-configuration/jwks"));
app.Run();
