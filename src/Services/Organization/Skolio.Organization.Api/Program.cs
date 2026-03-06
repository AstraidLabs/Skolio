using Microsoft.AspNetCore.Authentication.JwtBearer;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Api.Configuration;
using Skolio.Organization.Application;
using Skolio.Organization.Infrastructure;
using Skolio.Organization.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OrganizationServiceOptions>()
    .Bind(builder.Configuration.GetSection(OrganizationServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtValidationOptions>()
    .Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? throw new InvalidOperationException("Missing Organization auth options.");

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
    options.AddPolicy(SkolioPolicies.SchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator"));
    options.AddPolicy(SkolioPolicies.TeacherOrSchoolAdministration, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher"));
    options.AddPolicy(SkolioPolicies.ParentStudentTeacherRead, policy => policy.RequireRole("PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"));
});

builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddHealthChecks();
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

if (app.Environment.IsDevelopment())
{
    await app.ApplyOrganizationMigrationsAsync();
    app.MapOpenApi();
}

app.UseCors("SkolioDevelopment");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<OrganizationServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "phase-5-auth-ready",
        publicBaseUrl = options.Value.PublicBaseUrl
    });
});

app.Run();
