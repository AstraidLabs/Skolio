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

    private static readonly SeedUserDefinition KindergartenTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000102"),
        "kindergarten.teacher@skolio.local",
        "Kindergarten",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition KindergartenParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000103"),
        "kindergarten.parent@skolio.local",
        "Kindergarten",
        "Parent",
        UserType.Parent,
        "Parent",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition KindergartenStudent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000104"),
        "kindergarten.child@skolio.local",
        "Kindergarten",
        "Child",
        UserType.Student,
        "Student",
        KindergartenSchoolId);

    private static readonly SeedUserDefinition ElementarySchoolAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000201"),
        "elementary.admin@skolio.local",
        "Elementary",
        "Administrator",
        UserType.SchoolAdministrator,
        "SchoolAdministrator",
        ElementarySchoolId);

    private static readonly SeedUserDefinition ElementaryTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000202"),
        "elementary.teacher@skolio.local",
        "Elementary",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        ElementarySchoolId);

    private static readonly SeedUserDefinition ElementaryParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000203"),
        "elementary.parent@skolio.local",
        "Elementary",
        "Parent",
        UserType.Parent,
        "Parent",
        ElementarySchoolId);

    private static readonly SeedUserDefinition ElementaryStudent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000204"),
        "elementary.student@skolio.local",
        "Elementary",
        "Student",
        UserType.Student,
        "Student",
        ElementarySchoolId);

    private static readonly SeedUserDefinition SecondarySchoolAdministrator = new(
        Guid.Parse("10000000-0000-0000-0000-000000000301"),
        "secondary.admin@skolio.local",
        "Secondary",
        "Administrator",
        UserType.SchoolAdministrator,
        "SchoolAdministrator",
        SecondarySchoolId);

    private static readonly SeedUserDefinition SecondaryTeacher = new(
        Guid.Parse("10000000-0000-0000-0000-000000000302"),
        "secondary.teacher@skolio.local",
        "Secondary",
        "Teacher",
        UserType.Teacher,
        "Teacher",
        SecondarySchoolId);

    private static readonly SeedUserDefinition SecondaryParent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000303"),
        "secondary.parent@skolio.local",
        "Secondary",
        "Parent",
        UserType.Parent,
        "Parent",
        SecondarySchoolId);

    private static readonly SeedUserDefinition SecondaryStudent = new(
        Guid.Parse("10000000-0000-0000-0000-000000000304"),
        "secondary.student@skolio.local",
        "Secondary",
        "Student",
        UserType.Student,
        "Student",
        SecondarySchoolId);

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
        KindergartenTeacher,
        KindergartenParent,
        KindergartenStudent,
        ElementarySchoolAdministrator,
        ElementaryTeacher,
        ElementaryParent,
        ElementaryStudent,
        SecondarySchoolAdministrator,
        SecondaryTeacher,
        SecondaryParent,
        SecondaryStudent
    ];

    private static readonly ParentStudentLinkDefinition[] SeedParentStudentLinks =
    [
        new(KindergartenParent.UserProfileId, KindergartenStudent.UserProfileId, "Parent"),
        new(ElementaryParent.UserProfileId, ElementaryStudent.UserProfileId, "Parent"),
        new(SecondaryParent.UserProfileId, SecondaryStudent.UserProfileId, "Parent")
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
            var identityUser = await EnsureIdentityUserAsync(seedUser, seedPassword);
            await EnsureRoleMembershipAsync(identityUser, seedUser);
            await EnsureUserProfileAsync(seedUser, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SkolioIdentityUser> EnsureIdentityUserAsync(SeedUserDefinition seedUser, string seedPassword)
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
            return user;
        }

        var requiresUpdate = false;
        if (!string.Equals(user.UserName, seedUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = seedUser.Email;
            requiresUpdate = true;
        }

        if (!string.Equals(user.Email, seedUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = seedUser.Email;
            requiresUpdate = true;
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            requiresUpdate = true;
        }

        if (requiresUpdate)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update seed user {seedUser.Email}: {string.Join(",", updateResult.Errors.Select(x => x.Code))}");
            }

            logger.LogInformation("Seed identity account normalized: {Email}", seedUser.Email);
        }
        else
        {
            logger.LogInformation("Seed identity account already exists: {Email}", seedUser.Email);
        }

        return user;
    }

    private async Task EnsureRoleMembershipAsync(SkolioIdentityUser user, SeedUserDefinition seedUser)
    {
        if (await userManager.IsInRoleAsync(user, seedUser.RoleCode))
        {
            logger.LogInformation("Seed identity role already assigned: {Email} -> {RoleCode}", seedUser.Email, seedUser.RoleCode);
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, seedUser.RoleCode);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to assign role {seedUser.RoleCode} for {seedUser.Email}: {string.Join(",", roleResult.Errors.Select(x => x.Code))}");
        }

        logger.LogInformation("Seed identity role assigned: {Email} -> {RoleCode}", seedUser.Email, seedUser.RoleCode);
    }

    private async Task EnsureUserProfileAsync(SeedUserDefinition seedUser, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == seedUser.UserProfileId, cancellationToken);

        if (profile is null)
        {
            dbContext.UserProfiles.Add(UserProfile.Create(
                seedUser.UserProfileId,
                seedUser.FirstName,
                seedUser.LastName,
                seedUser.UserType,
                seedUser.Email,
                BuildPreferredDisplayName(seedUser.FirstName, seedUser.LastName),
                BuildPreferredLanguage(seedUser.UserType),
                BuildPhoneNumber(seedUser.UserType),
                BuildGender(seedUser.UserType),
                BuildDateOfBirth(seedUser.UserType),
                BuildNationalIdNumber(seedUser.UserType),
                BuildBirthPlace(),
                BuildPermanentAddress(seedUser.UserType),
                BuildCorrespondenceAddress(seedUser.UserType),
                seedUser.Email,
                BuildLegalGuardian1(seedUser.UserType),
                BuildLegalGuardian2(seedUser.UserType),
                BuildSchoolPlacement(seedUser.UserType),
                BuildHealthInsuranceProvider(seedUser.UserType),
                BuildPediatrician(seedUser.UserType),
                BuildHealthSafetyNotes(seedUser.UserType),
                BuildSupportMeasuresSummary(seedUser.UserType),
                BuildPositionTitle(seedUser.UserType),
                BuildTeacherRoleLabel(seedUser.UserType),
                BuildQualificationSummary(seedUser.UserType),
                BuildSchoolContextSummary(seedUser.UserType),
                BuildParentRelationshipSummary(seedUser.UserType),
                BuildDeliveryContactName(seedUser.UserType),
                BuildDeliveryContactPhone(seedUser.UserType),
                BuildPreferredContactChannel(seedUser.UserType),
                BuildCommunicationPreferencesSummary(seedUser.UserType),
                BuildPublicContactNote(seedUser.UserType),
                BuildPreferredContactNote(seedUser.UserType)));

            logger.LogInformation("Seed user profile created: {Email}", seedUser.Email);
            return;
        }

        profile.Update(
            seedUser.FirstName,
            seedUser.LastName,
            seedUser.UserType,
            seedUser.Email,
            BuildPreferredDisplayName(seedUser.FirstName, seedUser.LastName),
            BuildPreferredLanguage(seedUser.UserType),
            BuildPhoneNumber(seedUser.UserType),
            BuildGender(seedUser.UserType),
            BuildDateOfBirth(seedUser.UserType),
            BuildNationalIdNumber(seedUser.UserType),
            BuildBirthPlace(),
            BuildPermanentAddress(seedUser.UserType),
            BuildCorrespondenceAddress(seedUser.UserType),
            seedUser.Email,
            BuildLegalGuardian1(seedUser.UserType),
            BuildLegalGuardian2(seedUser.UserType),
            BuildSchoolPlacement(seedUser.UserType),
            BuildHealthInsuranceProvider(seedUser.UserType),
            BuildPediatrician(seedUser.UserType),
            BuildHealthSafetyNotes(seedUser.UserType),
            BuildSupportMeasuresSummary(seedUser.UserType),
            BuildPositionTitle(seedUser.UserType),
            BuildTeacherRoleLabel(seedUser.UserType),
            BuildQualificationSummary(seedUser.UserType),
            BuildSchoolContextSummary(seedUser.UserType),
            BuildParentRelationshipSummary(seedUser.UserType),
            BuildDeliveryContactName(seedUser.UserType),
            BuildDeliveryContactPhone(seedUser.UserType),
            BuildPreferredContactChannel(seedUser.UserType),
            BuildCommunicationPreferencesSummary(seedUser.UserType),
            BuildPublicContactNote(seedUser.UserType),
            BuildPreferredContactNote(seedUser.UserType));

        profile.Activate();
        logger.LogInformation("Seed user profile already exists and was refreshed: {Email}", seedUser.Email);
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
                logger.LogInformation("Seed role assignment already exists: {Email} -> {RoleCode} ({SchoolId})", seedUser.Email, seedUser.RoleCode, schoolId);
                continue;
            }

            dbContext.SchoolRoleAssignments.Add(SchoolRoleAssignment.Create(Guid.NewGuid(), seedUser.UserProfileId, schoolId, seedUser.RoleCode));
            logger.LogInformation("Seed role assignment created: {Email} -> {RoleCode} ({SchoolId})", seedUser.Email, seedUser.RoleCode, schoolId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureParentStudentLinksAsync(CancellationToken cancellationToken)
    {
        foreach (var link in SeedParentStudentLinks)
        {
            var exists = await dbContext.ParentStudentLinks.AnyAsync(
                x => x.ParentUserProfileId == link.ParentUserProfileId && x.StudentUserProfileId == link.StudentUserProfileId,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation("Seed parent-student link already exists: {Parent} -> {Student}", link.ParentUserProfileId, link.StudentUserProfileId);
                continue;
            }

            dbContext.ParentStudentLinks.Add(ParentStudentLink.Create(Guid.NewGuid(), link.ParentUserProfileId, link.StudentUserProfileId, link.Relationship));
            logger.LogInformation("Seed parent-student link created: {Parent} -> {Student}", link.ParentUserProfileId, link.StudentUserProfileId);
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

    private sealed record ParentStudentLinkDefinition(
        Guid ParentUserProfileId,
        Guid StudentUserProfileId,
        string Relationship);

    private static string BuildPreferredDisplayName(string firstName, string lastName) => $"{firstName} {lastName}";

    private static string BuildPreferredLanguage(UserType userType)
        => userType switch
        {
            UserType.Parent => "cs-CZ",
            UserType.Student => "cs-CZ",
            UserType.Teacher => "cs-CZ",
            UserType.SchoolAdministrator => "cs-CZ",
            UserType.SupportStaff => "en-US",
            _ => "cs-CZ"
        };

    private static string BuildPhoneNumber(UserType userType)
        => userType switch
        {
            UserType.SupportStaff => "+420700100001",
            UserType.SchoolAdministrator => "+420700100101",
            UserType.Teacher => "+420700100201",
            UserType.Parent => "+420700100301",
            UserType.Student => "+420700100401",
            _ => "+420700199999"
        };

    private static string? BuildPositionTitle(UserType userType)
        => userType switch
        {
            UserType.SchoolAdministrator => "SCHOOL_ADMINISTRATOR",
            UserType.Teacher => "TEACHER",
            _ => null
        };

    private static string? BuildTeacherRoleLabel(UserType userType)
        => userType switch
        {
            UserType.SchoolAdministrator => "School administration lead",
            UserType.Teacher => "Classroom and subject teacher",
            _ => null
        };

    private static string? BuildQualificationSummary(UserType userType)
        => userType switch
        {
            UserType.Teacher => "Pedagogical qualification validated by school administration.",
            UserType.SchoolAdministrator => "School management qualification validated by platform governance.",
            _ => null
        };

    private static string? BuildSchoolContextSummary(UserType userType)
        => userType switch
        {
            UserType.Teacher => "Assigned to active school context and teaching assignments maintained in Organization service.",
            UserType.SchoolAdministrator => "Responsible for school-scoped administration in active context.",
            _ => null
        };

    private static string? BuildParentRelationshipSummary(UserType userType)
        => userType == UserType.Parent
            ? "Parent profile linked to students through identity-managed parent-student relationships."
            : null;

    private static string? BuildDeliveryContactName(UserType userType)
        => userType == UserType.Parent ? "Primary Parent Contact" : null;

    private static string? BuildDeliveryContactPhone(UserType userType)
        => userType == UserType.Parent ? "+420700100301" : null;

    private static string? BuildPreferredContactChannel(UserType userType)
        => userType == UserType.Parent ? "EMAIL" : null;

    private static string? BuildCommunicationPreferencesSummary(UserType userType)
        => userType == UserType.Parent ? "School communication preferred via email notifications." : null;

    private static string? BuildPublicContactNote(UserType userType)
        => userType == UserType.Teacher ? "Consultation hours via school communication channel." : null;

    private static string? BuildPreferredContactNote(UserType userType)
        => userType == UserType.Parent ? "Preferred communication in Czech language." : null;

    private static string? BuildGender(UserType userType)
        => userType == UserType.Student ? "NotSpecified" : null;

    private static DateOnly? BuildDateOfBirth(UserType userType)
        => userType == UserType.Student ? new DateOnly(2012, 9, 1) : null;

    private static string? BuildNationalIdNumber(UserType userType)
        => userType == UserType.Student ? "120901/0001" : null;

    private static string BuildBirthPlace() => "Brno";

    private static string? BuildPermanentAddress(UserType userType)
        => userType == UserType.Student ? "Brno, Czech Republic" : null;

    private static string? BuildCorrespondenceAddress(UserType userType)
        => userType == UserType.Student ? "Brno, Czech Republic" : null;

    private static string? BuildLegalGuardian1(UserType userType)
        => userType == UserType.Student ? "Primary Parent Contact" : null;

    private static string? BuildLegalGuardian2(UserType userType)
        => userType == UserType.Student ? "Secondary Parent Contact" : null;

    private static string? BuildSchoolPlacement(UserType userType)
        => userType == UserType.Student ? "Class placement according to school year" : null;

    private static string? BuildHealthInsuranceProvider(UserType userType)
        => userType == UserType.Student ? "VZP" : null;

    private static string? BuildPediatrician(UserType userType)
        => userType == UserType.Student ? "MUDr. Novak" : null;

    private static string? BuildHealthSafetyNotes(UserType userType)
        => userType == UserType.Student ? "No known severe conditions." : null;

    private static string? BuildSupportMeasuresSummary(UserType userType)
        => userType == UserType.Student ? "Basic in-class support plan." : null;
}
