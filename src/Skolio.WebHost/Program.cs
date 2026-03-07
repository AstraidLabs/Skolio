using Microsoft.AspNetCore.HttpOverrides;
using Skolio.WebHost.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<FrontendHostOptions>()
    .Bind(builder.Configuration.GetSection(FrontendHostOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ServiceEndpointsOptions>()
    .Bind(builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.Logger.LogInformation("Starting {ServiceName} in {Environment}.", "Skolio.WebHost", app.Environment.EnvironmentName);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/bootstrap-config", (Microsoft.Extensions.Options.IOptions<ServiceEndpointsOptions> endpoints) =>
{
    return Results.Ok(new
    {
        identityAuthority = endpoints.Value.IdentityApi,
        oidcClientId = endpoints.Value.OidcClientId,
        oidcRedirectUri = endpoints.Value.OidcRedirectUri,
        oidcPostLogoutRedirectUri = endpoints.Value.OidcPostLogoutRedirectUri,
        oidcScope = endpoints.Value.OidcScope,
        organizationApi = endpoints.Value.OrganizationApi,
        academicsApi = endpoints.Value.AcademicsApi,
        communicationApi = endpoints.Value.CommunicationApi,
        administrationApi = endpoints.Value.AdministrationApi
    });
}).WithName("SkolioBootstrapConfig");

app.MapRazorPages();
app.MapFallbackToPage("/AppHost");

app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Stopping {ServiceName}.", "Skolio.WebHost"));
app.Run();

