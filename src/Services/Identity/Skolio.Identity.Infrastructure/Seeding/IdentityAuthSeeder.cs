using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Configuration;

namespace Skolio.Identity.Infrastructure.Seeding;

public sealed class IdentityAuthSeeder(
    RoleManager<SkolioIdentityRole> roleManager,
    UserManager<SkolioIdentityUser> userManager,
    IConfiguration configuration,
    IOpenIddictApplicationManager applicationManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        string[] roles = ["PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new SkolioIdentityRole { Name = role });
            }
        }

        var adminEmail = configuration["Identity:Seed:AdminEmail"] ?? "platform.admin@skolio.local";
        var adminPassword = configuration["Identity:Seed:AdminPassword"] ?? "SkolioAdmin!2026";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new SkolioIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed admin user: {string.Join(",", result.Errors.Select(e => e.Code))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "PlatformAdministrator"))
        {
            await userManager.AddToRoleAsync(admin, "PlatformAdministrator");
        }

        var oidcOptions = configuration.GetSection(OpenIddictOptions.SectionName).Get<OpenIddictOptions>() ?? throw new InvalidOperationException("Missing OpenIddict options for seeding.");

        if (await applicationManager.FindByClientIdAsync(oidcOptions.FrontendClient.ClientId, cancellationToken) is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = oidcOptions.FrontendClient.ClientId,
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                DisplayName = oidcOptions.FrontendClient.DisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public
            };

            foreach (var redirectUri in oidcOptions.FrontendClient.RedirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(redirectUri));
            }

            foreach (var postLogoutRedirectUri in oidcOptions.FrontendClient.PostLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri));
            }

            descriptor.Permissions.UnionWith(new[]
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Openid,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "skolio_api"
            });

            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }
    }
}
