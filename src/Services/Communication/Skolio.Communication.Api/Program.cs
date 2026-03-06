using Microsoft.AspNetCore.Authentication.JwtBearer;
using Skolio.Communication.Api.Auth;
using Skolio.Communication.Api.Configuration;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application;
using Skolio.Communication.Infrastructure;
using Skolio.Communication.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<CommunicationServiceOptions>().Bind(builder.Configuration.GetSection(CommunicationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<JwtValidationOptions>().Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? throw new InvalidOperationException("Missing Communication auth options.");
builder.Services.AddCommunicationApplication();builder.Services.AddCommunicationInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { options.Authority = jwtOptions.Authority; options.Audience = jwtOptions.Audience; options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SkolioPolicies.PlatformAdministration, policy => policy.RequireRole("PlatformAdministrator"));
    options.AddPolicy(SkolioPolicies.SchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));
});
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddHealthChecks();builder.Services.AddSignalR();builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyCommunicationMigrationsAsync(); app.MapOpenApi(); }
app.UseCors("SkolioDevelopment");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.MapHealthChecks("/health");app.MapHub<CommunicationHub>("/hubs/communication").RequireAuthorization(SkolioPolicies.ParentStudentTeacherRead);
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<CommunicationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-5-auth-ready", publicBaseUrl = options.Value.PublicBaseUrl, signalRHub = "/hubs/communication" }));
app.Run();
