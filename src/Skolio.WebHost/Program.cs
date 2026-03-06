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

app.UseStaticFiles();
app.MapHealthChecks("/health");
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
});

app.MapRazorPages();
app.MapFallbackToPage("/AppHost");

app.Run();
