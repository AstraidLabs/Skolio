using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Configuration;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Infrastructure.Seeding;

public sealed class IdentityAuthSeeder(
    RoleManager<SkolioIdentityRole> roleManager,
    UserManager<SkolioIdentityUser> userManager,
    IdentityDbContext dbContext,
    IConfiguration configuration,
    ILogger<IdentityAuthSeeder> logger,
    IOpenIddictApplicationManager applicationManager)
{
    private const string DefaultSeedPassword = "SkolioDev!2026";

    private static readonly Guid KindergartenSchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ElementarySchoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SecondarySchoolId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly SeedUserDefinition PlatformAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        "platform.admin@skolio.local",
        "Platform",
        "Administrator",
        UserType.SupportStaff,
        "PlatformAdministrator",
        null);

    private static readonly SeedUserDefinition KindergartenSchoolAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000101"),
        "kindergarten.admin@skolio.local",
        "Kindergarten",
        "Administrator",
        UserType.SchoolAdministrator,
        "SchoolAdministrator",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition ElementarySchoolAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000201"),
        "elementary.admin@skolio.local",
        "Elementary",
        "Administrator",
        UserType.SchoolAdministrator,
        "SchoolAdministrator",
        ElementarySchoolId);

    private static readonly SeedUserDefinition SecondarySchoolAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000301"),
        "secondary.admin@skolio.local",
        "Secondary",
        "Administrator",
        UserType.SchoolAdministrator,
        "SchoolAdministrator",
        SecondarySchoolId);

    private static readonly SeedUserDefinition KindergartenTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000102"),
        "kindergarten.teacher@skolio.local",
        "Kindergarten",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition ElementaryTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000202"),
        "elementary.teacher@skolio.local",
        "Elementary",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        ElementarySchoolId);

    private static readonly SeedUserDefinition SecondaryTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000302"),
        "secondary.teacher@skolio.local",
        "Secondary",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        SecondarySchoolId);

    private static readonly SeedUserDefinition KindergartenParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000103"),
        "kindergarten.parent@skolio.local",
        "Kindergarten",
        "Parent",
        UserType.Parent,
        "Parent",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition ElementaryParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000203"),
        "elementary.parent@skolio.local",
        "Elementary",
        "Parent",
        UserType.Parent,
        "Parent",
        ElementarySchoolId);

    private static readonly SeedUserDefinition SecondaryParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000303"),
        "secondary.parent@skolio.local",
        "Secondary",
        "Parent",
        UserType.Parent,
        "Parent",
        SecondarySchoolId);

    private static readonly SeedUserDefinition ElementaryStudent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000204"),
        "elementary.student@skolio.local",
        "Elementary",
        "Student",
        UserType.Student,
        "Student",
        ElementarySchoolId);

    private static readonly SeedUserDefinition SecondaryStudent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000304"),
        "secondary.student@skolio.local",
        "Secondary",
        "Student",
        UserType.Student,
        "Student",
        SecondarySchoolId);

    private static readonly SeedProfileDefinition KindergartenChildProfile = new(
        Guid.Parse("10000000-0000-0000-0000-000000000104"),
        "Kindergarten",
        "Child",
        UserType.Student,
        "kindergarten.child@skolio.local");

    private static readonly string[] SeedRoles =
    [
        "PlatformAdministrator",
        "SchoolAdministrator",
        "Teacher",
        "Parent",
        "Student"
    ];

    private static readonly SeedUserDefinition[] SeedUsers =
    [
        PlatformAdministrator,
        KindergartenSchoolAdministrator,
        ElementarySchoolAdministrator,
        SecondarySchoolAdministrator,
        KindergartenTeacher,
        ElementaryTeacher,
        SecondaryTeacher,
        KindergartenParent,
        ElementaryParent,
        SecondaryParent,
        ElementaryStudent,
        SecondaryStudent
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Identity:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Identity seed is disabled by configuration.");
            return;
        }

        logger.LogInformation("Identity seed started.");
        await EnsureRolesAsync(cancellationToken);
        await EnsureUsersAndProfilesAsync(cancellationToken);
        await EnsureRoleAssignmentsAsync(cancellationToken);
        await EnsureParentStudentLinksAsync(cancellationToken);
        await EnsureOidcClientAsync(cancellationToken);
        logger.LogInformation("Identity seed completed.");
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in SeedRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation("Identity role already exists: {RoleName}", roleName);
                continue;
            }

            var result = await roleManager.CreateAsync(new SkolioIdentityRole { Name = roleName });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role {roleName}: {string.Join(",", result.Errors.Select(x => x.Code))}");
            }

            logger.LogInformation("Identity role created: {RoleName}", roleName);
        }
    }

    private async Task EnsureUsersAndProfilesAsync(CancellationToken cancellationToken)
    {
        var seedPassword = configuration["Identity:Seed:Password"] ?? DefaultSeedPassword;

        foreach (var seedUser in SeedUsers)
        {
            var user = await userManager.FindByEmailAsync(seedUser.Email);
            if (user is null)
            {
                user = new SkolioIdentityUser
                {
                    Id = seedUser.UserProfileId.ToString(),
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, seedPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create seed user {seedUser.Email}: {string.Join(",", createResult.Errors.Select(x => x.Code))}");
                }

                logger.LogInformation("Seed identity account created: {Email}", seedUser.Email);
            }
            else
            {
                logger.LogInformation("Seed identity account already exists: {Email}", seedUser.Email);
            }

            if (!await userManager.IsInRoleAsync(user, seedUser.RoleCode))
            {
                var roleResult = await userManager.AddToRoleAsync(user, seedUser.RoleCode);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign role {seedUser.RoleCode} for {seedUser.Email}: {string.Join(",", roleResult.Errors.Select(x => x.Code))}");
                }

                logger.LogInformation("Seed identity role assigned: {Email} -> {RoleCode}", seedUser.Email, seedUser.RoleCode);
            }

            var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == seedUser.UserProfileId, cancellationToken);
            if (profile is null)
            {
                dbContext.UserProfiles.Add(UserProfile.Create(seedUser.UserProfileId, seedUser.FirstName, seedUser.LastName, seedUser.UserType, seedUser.Email));
                logger.LogInformation("Seed user profile created: {Email}", seedUser.Email);
            }
            else
            {
                profile.Update(seedUser.FirstName, seedUser.LastName, seedUser.UserType, seedUser.Email);
                profile.Activate();
                logger.LogInformation("Seed user profile already exists and was refreshed: {Email}", seedUser.Email);
            }
        }

        var kindergartenChild = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == KindergartenChildProfile.UserProfileId, cancellationToken);
        if (kindergartenChild is null)
        {
            dbContext.UserProfiles.Add(UserProfile.Create(
                KindergartenChildProfile.UserProfileId,
                KindergartenChildProfile.FirstName,
                KindergartenChildProfile.LastName,
                KindergartenChildProfile.UserType,
                KindergartenChildProfile.Email));
            logger.LogInformation("Seed kindergarten child profile created for parent-child link coverage.");
        }
        else
        {
            kindergartenChild.Update(
                KindergartenChildProfile.FirstName,
                KindergartenChildProfile.LastName,
                KindergartenChildProfile.UserType,
                KindergartenChildProfile.Email);
            kindergartenChild.Activate();
            logger.LogInformation("Seed kindergarten child profile already exists and was refreshed.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoleAssignmentsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedUser in SeedUsers)
        {
            if (!seedUser.SchoolId.HasValue)
            {
                continue;
            }

            var schoolId = seedUser.SchoolId.Value;
            var exists = await dbContext.SchoolRoleAssignments.AnyAsync(
                x => x.UserProfileId == seedUser.UserProfileId
                     && x.SchoolId == schoolId
                     && x.RoleCode == seedUser.RoleCode,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation("Seed role assignment already exists: {Email} -> {RoleCode} ({SchoolId})", seedUser.Email, seedUser.RoleCode, seedUser.SchoolId);
                continue;
            }

            dbContext.SchoolRoleAssignments.Add(SchoolRoleAssignment.Create(Guid.NewGuid(), seedUser.UserProfileId, schoolId, seedUser.RoleCode));
            logger.LogInformation("Seed role assignment created: {Email} -> {RoleCode} ({SchoolId})", seedUser.Email, seedUser.RoleCode, schoolId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureParentStudentLinksAsync(CancellationToken cancellationToken)
    {
        var links = new[]
        {
            new { Parent = ElementaryParent.UserProfileId, Student = ElementaryStudent.UserProfileId, Relationship = "Parent" },
            new { Parent = SecondaryParent.UserProfileId, Student = SecondaryStudent.UserProfileId, Relationship = "Parent" },
            new { Parent = KindergartenParent.UserProfileId, Student = KindergartenChildProfile.UserProfileId, Relationship = "Parent" }
        };

        foreach (var link in links)
        {
            var exists = await dbContext.ParentStudentLinks.AnyAsync(
                x => x.ParentUserProfileId == link.Parent && x.StudentUserProfileId == link.Student,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation("Seed parent-student link already exists: {Parent} -> {Student}", link.Parent, link.Student);
                continue;
            }

            dbContext.ParentStudentLinks.Add(ParentStudentLink.Create(Guid.NewGuid(), link.Parent, link.Student, link.Relationship));
            logger.LogInformation("Seed parent-student link created: {Parent} -> {Student}", link.Parent, link.Student);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private sealed record SeedUserDefinition(
        Guid UserProfileId,
        string Email,
        string FirstName,
        string LastName,
        UserType UserType,
        string RoleCode,
        Guid? SchoolId);

    private sealed record SeedProfileDefinition(
        Guid UserProfileId,
        string FirstName,
        string LastName,
        UserType UserType,
        string Email);
}
