using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Auth;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Infrastructure.Seeding;

public sealed class IdentityDevelopmentAccountSeeder(
    UserManager<SkolioIdentityUser> userManager,
    IPasswordHasher<SkolioIdentityUser> passwordHasher,
    IdentityDbContext dbContext,
    IConfiguration configuration,
    ILogger<IdentityDevelopmentAccountSeeder> logger)
{
    private static readonly Guid KindergartenSchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ElementarySchoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SecondarySchoolId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly SeedAccountDefinition PlatformAdministrator = new(
        Guid.Parse("90000000-0000-0000-0000-000000000001"),
        "platform.admin@skolio.local",
        "platform.admin@skolio.local",
        "Platform",
        "Administrator",
        "Platform Administrator",
        "PlatformAdministrator",
        UserType.SupportStaff,
        null,
        "cs",
        "+420700000001",
        "Platform Administrator",
        null,
        "Platform administration");

    private static readonly SeedAccountDefinition KindergartenAdministrator = new(
        Guid.Parse("90000000-0000-0000-0000-000000000101"),
        "kindergarten.admin@skolio.local",
        "kindergarten.admin@skolio.local",
        "Kindergarten",
        "Administrator",
        "Kindergarten Administrator",
        "SchoolAdministrator",
        UserType.SchoolAdministrator,
        KindergartenSchoolId,
        "cs",
        "+420700000101",
        "School Administrator",
        "Skolio Kindergarten Brno",
        "Kindergarten administration");

    private static readonly SeedAccountDefinition ElementaryAdministrator = new(
        Guid.Parse("90000000-0000-0000-0000-000000000201"),
        "elementary.admin@skolio.local",
        "elementary.admin@skolio.local",
        "Elementary",
        "Administrator",
        "Elementary Administrator",
        "SchoolAdministrator",
        UserType.SchoolAdministrator,
        ElementarySchoolId,
        "cs",
        "+420700000201",
        "School Administrator",
        "Skolio Elementary Prague",
        "Elementary school administration");

    private static readonly SeedAccountDefinition SecondaryAdministrator = new(
        Guid.Parse("90000000-0000-0000-0000-000000000301"),
        "secondary.admin@skolio.local",
        "secondary.admin@skolio.local",
        "Secondary",
        "Administrator",
        "Secondary Administrator",
        "SchoolAdministrator",
        UserType.SchoolAdministrator,
        SecondarySchoolId,
        "cs",
        "+420700000301",
        "School Administrator",
        "Skolio Secondary Ostrava",
        "Secondary school administration");

    private static readonly SeedAccountDefinition KindergartenTeacher = new(
        Guid.Parse("90000000-0000-0000-0000-000000000102"),
        "kindergarten.teacher@skolio.local",
        "kindergarten.teacher@skolio.local",
        "Kindergarten",
        "Teacher",
        "Kindergarten Teacher",
        "Teacher",
        UserType.Teacher,
        KindergartenSchoolId,
        "cs",
        "+420700000102",
        "Teacher",
        "Skolio Kindergarten Brno",
        "Daily operations teacher");

    private static readonly SeedAccountDefinition ElementaryTeacher = new(
        Guid.Parse("90000000-0000-0000-0000-000000000202"),
        "elementary.teacher@skolio.local",
        "elementary.teacher@skolio.local",
        "Elementary",
        "Teacher",
        "Elementary Teacher",
        "Teacher",
        UserType.Teacher,
        ElementarySchoolId,
        "cs",
        "+420700000202",
        "Teacher",
        "Skolio Elementary Prague",
        "Class and subject teacher");

    private static readonly SeedAccountDefinition SecondaryTeacher = new(
        Guid.Parse("90000000-0000-0000-0000-000000000302"),
        "secondary.teacher@skolio.local",
        "secondary.teacher@skolio.local",
        "Secondary",
        "Teacher",
        "Secondary Teacher",
        "Teacher",
        UserType.Teacher,
        SecondarySchoolId,
        "cs",
        "+420700000302",
        "Teacher",
        "Skolio Secondary Ostrava",
        "Secondary subject teacher");

    private static readonly SeedAccountDefinition KindergartenParent = new(
        Guid.Parse("90000000-0000-0000-0000-000000000103"),
        "kindergarten.parent@skolio.local",
        "kindergarten.parent@skolio.local",
        "Kindergarten",
        "Parent",
        "Kindergarten Parent",
        "Parent",
        UserType.Parent,
        KindergartenSchoolId,
        "cs",
        "+420700000103",
        null,
        "Skolio Kindergarten Brno",
        "Parent linked to kindergarten child");

    private static readonly SeedAccountDefinition ElementaryParent = new(
        Guid.Parse("90000000-0000-0000-0000-000000000203"),
        "elementary.parent@skolio.local",
        "elementary.parent@skolio.local",
        "Elementary",
        "Parent",
        "Elementary Parent",
        "Parent",
        UserType.Parent,
        ElementarySchoolId,
        "cs",
        "+420700000203",
        null,
        "Skolio Elementary Prague",
        "Parent linked to elementary student");

    private static readonly SeedAccountDefinition SecondaryParent = new(
        Guid.Parse("90000000-0000-0000-0000-000000000303"),
        "secondary.parent@skolio.local",
        "secondary.parent@skolio.local",
        "Secondary",
        "Parent",
        "Secondary Parent",
        "Parent",
        UserType.Parent,
        SecondarySchoolId,
        "cs",
        "+420700000303",
        null,
        "Skolio Secondary Ostrava",
        "Parent linked to secondary student");

    private static readonly SeedAccountDefinition ElementaryStudent = new(
        Guid.Parse("90000000-0000-0000-0000-000000000204"),
        "elementary.student@skolio.local",
        "elementary.student@skolio.local",
        "Elementary",
        "Student",
        "Elementary Student",
        "Student",
        UserType.Student,
        ElementarySchoolId,
        "cs",
        null,
        null,
        "Skolio Elementary Prague",
        "Elementary student");

    private static readonly SeedAccountDefinition SecondaryStudent = new(
        Guid.Parse("90000000-0000-0000-0000-000000000304"),
        "secondary.student@skolio.local",
        "secondary.student@skolio.local",
        "Secondary",
        "Student",
        "Secondary Student",
        "Student",
        UserType.Student,
        SecondarySchoolId,
        "cs",
        null,
        null,
        "Skolio Secondary Ostrava",
        "Secondary student");

    private static readonly ChildProfileDefinition KindergartenChild = new(
        Guid.Parse("90000000-0000-0000-0000-000000000104"),
        "Kindergarten",
        "Child",
        "Kindergarten Child",
        "kindergarten.child@skolio.local",
        KindergartenSchoolId,
        "Skolio Kindergarten Brno",
        "Parent managed kindergarten child");

    private static readonly SeedAccountDefinition[] LoginAccounts =
    [
        PlatformAdministrator,
        KindergartenAdministrator,
        ElementaryAdministrator,
        SecondaryAdministrator,
        KindergartenTeacher,
        ElementaryTeacher,
        SecondaryTeacher,
        KindergartenParent,
        ElementaryParent,
        SecondaryParent,
        ElementaryStudent,
        SecondaryStudent
    ];

    private static readonly SeedRoleAssignmentDefinition[] RoleAssignments =
    [
        new(Guid.Parse("91000000-0000-0000-0000-000000000101"), KindergartenAdministrator.Id, KindergartenSchoolId, "SchoolAdministrator"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000201"), ElementaryAdministrator.Id, ElementarySchoolId, "SchoolAdministrator"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000301"), SecondaryAdministrator.Id, SecondarySchoolId, "SchoolAdministrator"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000102"), KindergartenTeacher.Id, KindergartenSchoolId, "Teacher"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000202"), ElementaryTeacher.Id, ElementarySchoolId, "Teacher"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000302"), SecondaryTeacher.Id, SecondarySchoolId, "Teacher"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000103"), KindergartenParent.Id, KindergartenSchoolId, "Parent"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000203"), ElementaryParent.Id, ElementarySchoolId, "Parent"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000303"), SecondaryParent.Id, SecondarySchoolId, "Parent"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000204"), ElementaryStudent.Id, ElementarySchoolId, "Student"),
        new(Guid.Parse("91000000-0000-0000-0000-000000000304"), SecondaryStudent.Id, SecondarySchoolId, "Student")
    ];

    private static readonly ParentStudentLinkDefinition[] ParentStudentLinks =
    [
        new(Guid.Parse("92000000-0000-0000-0000-000000000103"), KindergartenParent.Id, KindergartenChild.Id, "Parent"),
        new(Guid.Parse("92000000-0000-0000-0000-000000000203"), ElementaryParent.Id, ElementaryStudent.Id, "Parent"),
        new(Guid.Parse("92000000-0000-0000-0000-000000000303"), SecondaryParent.Id, SecondaryStudent.Id, "Parent")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Identity:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Identity development account seed is disabled by configuration.");
            return;
        }

        var password = configuration.GetValue<string>("Identity:Seed:Password");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Identity development account seed requires Identity:Seed:Password.");
        }

        logger.LogInformation("Identity development account seed started.");

        foreach (var definition in LoginAccounts)
        {
            await EnsureIdentityUserAsync(definition, password, cancellationToken);
            await EnsureUserProfileAsync(definition, cancellationToken);
        }

        await EnsureChildProfileAsync(KindergartenChild, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var definition in LoginAccounts)
        {
            await EnsureAspNetRoleAssignmentAsync(definition, cancellationToken);
        }

        await EnsureSchoolRoleAssignmentsAsync(cancellationToken);
        await EnsureParentStudentLinksAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Identity development account seed completed.");
    }

    private async Task EnsureIdentityUserAsync(SeedAccountDefinition definition, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(definition.Email);
        if (user is null)
        {
            user = new SkolioIdentityUser
            {
                Id = definition.Id.ToString(),
                UserName = definition.UserName,
                Email = definition.Email,
                EmailConfirmed = true,
                AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active,
                ActivatedAtUtc = DateTimeOffset.UtcNow,
                InviteStatus = IdentityInviteStatus.Active,
                InviteConfirmedAtUtc = DateTimeOffset.UtcNow,
                OnboardingCompletedAtUtc = DateTimeOffset.UtcNow,
                LockoutEnabled = true,
                PhoneNumber = definition.PhoneNumber
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create development account {definition.Email}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }

            logger.LogInformation("Identity development account created: {Email}", definition.Email);
            return;
        }

        user.UserName = definition.UserName;
        user.Email = definition.Email;
        user.PhoneNumber = definition.PhoneNumber;
        user.EmailConfirmed = true;
        user.AccountLifecycleStatus = IdentityAccountLifecycleStatus.Active;
        user.ActivatedAtUtc ??= DateTimeOffset.UtcNow;
        user.InviteStatus = IdentityInviteStatus.Active;
        user.InviteConfirmedAtUtc ??= DateTimeOffset.UtcNow;
        user.OnboardingCompletedAtUtc ??= DateTimeOffset.UtcNow;
        user.DeactivatedAtUtc = null;
        user.DeactivationReason = null;
        user.DeactivatedByUserId = null;
        user.BlockedAtUtc = null;
        user.BlockedReason = null;
        user.BlockedByUserId = null;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.IsBootstrapPlatformAdministrator = false;
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to refresh development account {definition.Email}: {string.Join(", ", updateResult.Errors.Select(x => x.Description))}");
        }

        logger.LogInformation("Identity development account refreshed: {Email}", definition.Email);
    }

    private async Task EnsureUserProfileAsync(SeedAccountDefinition definition, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
        if (profile is null)
        {
            dbContext.UserProfiles.Add(UserProfile.Create(
                definition.Id,
                definition.FirstName,
                definition.LastName,
                definition.UserType,
                definition.Email,
                preferredDisplayName: definition.DisplayName,
                preferredLanguage: definition.PreferredLanguage,
                phoneNumber: definition.PhoneNumber,
                schoolPlacement: definition.SchoolContextSummary,
                positionTitle: definition.PositionTitle,
                teacherRoleLabel: definition.RoleCode == "Teacher" ? definition.PositionTitle : null,
                schoolContextSummary: definition.SchoolContextSummary,
                parentRelationshipSummary: definition.RoleCode == "Parent" ? definition.Summary : null,
                administrativeWorkDesignation: definition.RoleCode is "PlatformAdministrator" or "SchoolAdministrator" ? definition.PositionTitle : null,
                administrativeOrganizationSummary: definition.RoleCode == "SchoolAdministrator" ? definition.SchoolName : null,
                platformRoleContextSummary: definition.RoleCode == "PlatformAdministrator" ? definition.Summary : null,
                managedPlatformAreasSummary: definition.RoleCode == "PlatformAdministrator" ? "Identity, Organization, Academics, Communication, Administration" : null,
                administrativeBoundarySummary: definition.RoleCode == "SchoolAdministrator" ? definition.Summary : null));
            logger.LogInformation("Identity development profile created: {Email}", definition.Email);
            return;
        }

        profile.Update(
            definition.FirstName,
            definition.LastName,
            definition.UserType,
            definition.Email,
            definition.DisplayName,
            definition.PreferredLanguage,
            definition.PhoneNumber,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            definition.SchoolContextSummary,
            null,
            null,
            null,
            null,
            definition.PositionTitle,
            definition.RoleCode == "Teacher" ? definition.PositionTitle : null,
            null,
            definition.SchoolContextSummary,
            definition.RoleCode == "Parent" ? definition.Summary : null,
            null,
            null,
            null,
            null,
            null,
            null,
            definition.RoleCode is "PlatformAdministrator" or "SchoolAdministrator" ? definition.PositionTitle : null,
            definition.RoleCode == "SchoolAdministrator" ? definition.SchoolName : null,
            definition.RoleCode == "PlatformAdministrator" ? definition.Summary : null,
            definition.RoleCode == "PlatformAdministrator" ? "Identity, Organization, Academics, Communication, Administration" : null,
            definition.RoleCode == "SchoolAdministrator" ? definition.Summary : null);
        profile.Activate();
        logger.LogInformation("Identity development profile refreshed: {Email}", definition.Email);
    }

    private async Task EnsureChildProfileAsync(ChildProfileDefinition definition, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
        if (profile is null)
        {
            dbContext.UserProfiles.Add(UserProfile.Create(
                definition.Id,
                definition.FirstName,
                definition.LastName,
                UserType.Student,
                definition.Email,
                preferredDisplayName: definition.DisplayName,
                preferredLanguage: "cs",
                schoolPlacement: definition.SchoolContextSummary,
                schoolContextSummary: definition.SchoolContextSummary,
                parentRelationshipSummary: definition.Summary));
            logger.LogInformation("Identity kindergarten child profile created: {ProfileId}", definition.Id);
            return;
        }

        profile.Update(
            definition.FirstName,
            definition.LastName,
            UserType.Student,
            definition.Email,
            definition.DisplayName,
            "cs",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            definition.SchoolContextSummary,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            definition.SchoolContextSummary,
            definition.Summary,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        profile.Activate();
    }

    private async Task EnsureAspNetRoleAssignmentAsync(SeedAccountDefinition definition, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(definition.Email);
        if (user is null)
        {
            throw new InvalidOperationException($"Development account {definition.Email} is missing before role assignment.");
        }

        if (await userManager.IsInRoleAsync(user, definition.RoleCode))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, definition.RoleCode);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to assign role {definition.RoleCode} to {definition.Email}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
        }

        logger.LogInformation("Identity development role assigned: {Email} -> {Role}", definition.Email, definition.RoleCode);
    }

    private async Task EnsureSchoolRoleAssignmentsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in RoleAssignments)
        {
            var existing = await dbContext.SchoolRoleAssignments
                .FirstOrDefaultAsync(x => x.UserProfileId == definition.UserProfileId && x.SchoolId == definition.SchoolId && x.RoleCode == definition.RoleCode, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            dbContext.SchoolRoleAssignments.Add(SchoolRoleAssignment.Create(definition.Id, definition.UserProfileId, definition.SchoolId, definition.RoleCode));
            logger.LogInformation("Identity development school role assignment created: {UserProfileId} -> {Role} @ {SchoolId}", definition.UserProfileId, definition.RoleCode, definition.SchoolId);
        }
    }

    private async Task EnsureParentStudentLinksAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in ParentStudentLinks)
        {
            var existing = await dbContext.ParentStudentLinks
                .FirstOrDefaultAsync(x => x.ParentUserProfileId == definition.ParentUserProfileId && x.StudentUserProfileId == definition.StudentUserProfileId, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            dbContext.ParentStudentLinks.Add(ParentStudentLink.Create(definition.Id, definition.ParentUserProfileId, definition.StudentUserProfileId, definition.Relationship));
            logger.LogInformation("Identity development parent-student link created: {ParentUserProfileId} -> {StudentUserProfileId}", definition.ParentUserProfileId, definition.StudentUserProfileId);
        }
    }

    private sealed record SeedAccountDefinition(
        Guid Id,
        string Email,
        string UserName,
        string FirstName,
        string LastName,
        string DisplayName,
        string RoleCode,
        UserType UserType,
        Guid? SchoolId,
        string PreferredLanguage,
        string? PhoneNumber,
        string? PositionTitle,
        string? SchoolName,
        string Summary)
    {
        public string? SchoolContextSummary => SchoolId.HasValue && !string.IsNullOrWhiteSpace(SchoolName)
            ? $"{SchoolName} | {RoleCode}"
            : RoleCode == "PlatformAdministrator"
                ? "Platform context"
                : null;
    }

    private sealed record SeedRoleAssignmentDefinition(Guid Id, Guid UserProfileId, Guid SchoolId, string RoleCode);
    private sealed record ParentStudentLinkDefinition(Guid Id, Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
    private sealed record ChildProfileDefinition(Guid Id, string FirstName, string LastName, string DisplayName, string Email, Guid SchoolId, string SchoolName, string Summary)
    {
        public string SchoolContextSummary => $"{SchoolName} | Student";
    }
}
