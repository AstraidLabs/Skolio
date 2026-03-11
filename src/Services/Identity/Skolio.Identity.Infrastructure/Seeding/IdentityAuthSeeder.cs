using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Configuration;

namespace Skolio.Identity.Infrastructure.Seeding;

public sealed class IdentityAuthSeeder(
    RoleManager<SkolioIdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<IdentityAuthSeeder> logger,
    IOpenIddictApplicationManager applicationManager)
{
    private static readonly SeedRoleDefinition[] RequiredRoles =
    [
        new("PlatformAdministrator", isGlobal: true, isSchoolScoped: false, requiresSchoolContext: false, requiresRoleSpecificLinks: false, allowedForBootstrap: true, allowedForCreateUser: false, allowedForUserManagementEdit: true),
        new("SchoolAdministrator", isGlobal: false, isSchoolScoped: true, requiresSchoolContext: true, requiresRoleSpecificLinks: false, allowedForBootstrap: false, allowedForCreateUser: true, allowedForUserManagementEdit: true),
        new("Teacher", isGlobal: false, isSchoolScoped: true, requiresSchoolContext: true, requiresRoleSpecificLinks: true, allowedForBootstrap: false, allowedForCreateUser: true, allowedForUserManagementEdit: true),
        new("Parent", isGlobal: false, isSchoolScoped: true, requiresSchoolContext: true, requiresRoleSpecificLinks: true, allowedForBootstrap: false, allowedForCreateUser: true, allowedForUserManagementEdit: true),
        new("Student", isGlobal: false, isSchoolScoped: true, requiresSchoolContext: true, requiresRoleSpecificLinks: true, allowedForBootstrap: false, allowedForCreateUser: true, allowedForUserManagementEdit: true)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Identity:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Identity seed is disabled by configuration.");
            return;
        }

        logger.LogInformation("Identity system seed started.");

        var seedState = await EvaluateSeedStateAsync(cancellationToken);
        switch (seedState)
        {
            case SeedState.FullyInitialized:
                logger.LogInformation("Identity system seed skipped: mandatory role baseline already exists.");
                await EnsureOidcClientAsync(cancellationToken);
                return;
            case SeedState.Empty:
            case SeedState.PartiallyInitialized:
                await EnsureRolesAsync(cancellationToken);
                await EnsureOidcClientAsync(cancellationToken);
                logger.LogInformation("Identity system seed completed.");
                return;
            default:
                throw new InvalidOperationException("Identity seed aborted due to inconsistent role baseline state.");
        }
    }

    private async Task<SeedState> EvaluateSeedStateAsync(CancellationToken cancellationToken)
    {
        var existingRoles = await roleManager.Roles.Select(role => role.Name).Where(name => name != null).Select(name => name!).ToArrayAsync(cancellationToken);
        var requiredRoleNames = RequiredRoles.Select(role => role.RoleName).ToHashSet(StringComparer.Ordinal);
        var existingRoleNames = existingRoles.ToHashSet(StringComparer.Ordinal);

        var requiredCount = RequiredRoles.Length;
        var presentRequiredCount = RequiredRoles.Count(role => existingRoleNames.Contains(role.RoleName));

        if (presentRequiredCount == 0)
        {
            logger.LogInformation("Identity seed state detected as Empty.");
            return SeedState.Empty;
        }

        if (presentRequiredCount == requiredCount)
        {
            logger.LogInformation("Identity seed state detected as FullyInitialized.");
            LogRoleBoundaryMetadata();
            return SeedState.FullyInitialized;
        }

        if (presentRequiredCount > 0 && presentRequiredCount < requiredCount)
        {
            logger.LogWarning("Identity seed state detected as PartiallyInitialized. Missing mandatory roles will be created.");
            return SeedState.PartiallyInitialized;
        }

        var unsupportedRequiredRoles = requiredRoleNames.Except(existingRoleNames, StringComparer.Ordinal).ToArray();
        logger.LogError("Identity seed state is inconsistent. Missing required roles: {MissingRoles}", string.Join(", ", unsupportedRequiredRoles));
        return SeedState.Inconsistent;
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var role in RequiredRoles)
        {
            if (await roleManager.RoleExistsAsync(role.RoleName))
            {
                logger.LogInformation("Identity role already exists: {RoleName}", role.RoleName);
                continue;
            }

            var result = await roleManager.CreateAsync(new SkolioIdentityRole { Name = role.RoleName });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role {role.RoleName}: {string.Join(",", result.Errors.Select(x => x.Code))}");
            }

            logger.LogInformation("Identity role created: {RoleName}", role.RoleName);
        }

        LogRoleBoundaryMetadata();
    }

    private void LogRoleBoundaryMetadata()
    {
        foreach (var role in RequiredRoles)
        {
            logger.LogInformation(
                "Role baseline: role={RoleName}, global={IsGlobal}, schoolScoped={IsSchoolScoped}, requiresSchoolContext={RequiresSchoolContext}, requiresRoleSpecificLinks={RequiresRoleSpecificLinks}, bootstrapAllowed={BootstrapAllowed}, createUserAllowed={CreateUserAllowed}, userManagementEditAllowed={UserManagementEditAllowed}",
                role.RoleName,
                role.IsGlobal,
                role.IsSchoolScoped,
                role.RequiresSchoolContext,
                role.RequiresRoleSpecificLinks,
                role.AllowedForBootstrap,
                role.AllowedForCreateUser,
                role.AllowedForUserManagementEdit);
        }
    }

    private async Task EnsureOidcClientAsync(CancellationToken cancellationToken)
    {
        var oidcOptions = configuration.GetSection(OpenIddictOptions.SectionName).Get<OpenIddictOptions>()
                          ?? throw new InvalidOperationException("Missing OpenIddict options for seeding.");

        if (await applicationManager.FindByClientIdAsync(oidcOptions.FrontendClient.ClientId, cancellationToken) is not null)
        {
            logger.LogInformation("OpenIddict client already exists: {ClientId}", oidcOptions.FrontendClient.ClientId);
            return;
        }

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
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Prefixes.Scope + "skolio_api"
        });

        descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        logger.LogInformation("OpenIddict client created: {ClientId}", oidcOptions.FrontendClient.ClientId);
    }

    private sealed record SeedRoleDefinition(
        string RoleName,
        bool IsGlobal,
        bool IsSchoolScoped,
        bool RequiresSchoolContext,
        bool RequiresRoleSpecificLinks,
        bool AllowedForBootstrap,
        bool AllowedForCreateUser,
        bool AllowedForUserManagementEdit);

    private enum SeedState
    {
        Empty,
        PartiallyInitialized,
        FullyInitialized,
        Inconsistent
    }
}
