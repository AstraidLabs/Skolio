using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.ValueObjects;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Infrastructure.Seeding;

public sealed class OrganizationDevelopmentSeeder(
    OrganizationDbContext dbContext,
    IConfiguration configuration,
    ILogger<OrganizationDevelopmentSeeder> logger)
{
    private static readonly SchoolOperatorDefinition KindergartenOperator = new(
        Guid.Parse("41111111-1111-1111-1111-111111111111"),
        "Skolio Kindergarten Operations s.r.o.",
        LegalForm.LimitedLiabilityCompany,
        "12345670",
        Address.Create("Skolkova 1", "Brno", "60200", "CZ"),
        "RZ-KG-001",
        "Jana Novakova",
        "Jednatel");

    private static readonly SchoolOperatorDefinition ElementaryOperator = new(
        Guid.Parse("42222222-2222-2222-2222-222222222222"),
        "Skolio Elementary Operations a.s.",
        LegalForm.JointStockCompany,
        "22345671",
        Address.Create("Skolni 10", "Praha", "11000", "CZ"),
        "RZ-EL-001",
        "Petr Svoboda",
        "Predstavenstvo");

    private static readonly SchoolOperatorDefinition SecondaryOperator = new(
        Guid.Parse("43333333-3333-3333-3333-333333333333"),
        "Skolio Secondary Operations z.u.",
        LegalForm.NonProfitOrganization,
        "32345672",
        Address.Create("Akademicka 5", "Ostrava", "70200", "CZ"),
        "RZ-SE-001",
        "Milan Dvorak",
        "Spravni rada");

    private static readonly FounderDefinition KindergartenFounder = new(
        Guid.Parse("51111111-1111-1111-1111-111111111111"),
        FounderType.Municipality,
        FounderCategory.Public,
        "Mesto Brno",
        LegalForm.Municipality,
        "44992785",
        Address.Create("Dominikanske namesti 1", "Brno", "60200", "CZ"),
        "podatelna@brno.cz");

    private static readonly FounderDefinition ElementaryFounder = new(
        Guid.Parse("52222222-2222-2222-2222-222222222222"),
        FounderType.Region,
        FounderCategory.Public,
        "Hlavni mesto Praha",
        LegalForm.Region,
        "00064581",
        Address.Create("Marianske namesti 2", "Praha", "11000", "CZ"),
        "posta@praha.eu");

    private static readonly FounderDefinition SecondaryFounder = new(
        Guid.Parse("53333333-3333-3333-3333-333333333333"),
        FounderType.LegalEntity,
        FounderCategory.Private,
        "Skolio Education Foundation",
        LegalForm.NonProfitOrganization,
        "27345673",
        Address.Create("Nadrazni 45", "Ostrava", "70200", "CZ"),
        "contact@skolio.foundation");

    private static readonly SchoolDefinition KindergartenSchool = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Skolio Kindergarten Brno",
        SchoolType.Kindergarten,
        SchoolKind.General,
        "600001001",
        "kindergarten.brno@skolio.local",
        "+420541000101",
        "https://kindergarten.skolio.local",
        Address.Create("Skolkova 1", "Brno", "60200", "CZ"),
        "Main campus Brno-stred",
        new DateOnly(2018, 9, 1),
        new DateOnly(2019, 9, 1),
        220,
        "cs",
        KindergartenOperator.Id,
        KindergartenFounder.Id,
        PlatformStatus.Active);

    private static readonly SchoolDefinition ElementarySchool = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "Skolio Elementary Prague",
        SchoolType.ElementarySchool,
        SchoolKind.General,
        "600002002",
        "elementary.prague@skolio.local",
        "+420221000201",
        "https://elementary.skolio.local",
        Address.Create("Skolni 10", "Praha", "11000", "CZ"),
        "Primary campus Prague 1",
        new DateOnly(2016, 9, 1),
        new DateOnly(2017, 9, 1),
        540,
        "cs",
        ElementaryOperator.Id,
        ElementaryFounder.Id,
        PlatformStatus.Active);

    private static readonly SchoolDefinition SecondarySchool = new(
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "Skolio Secondary Ostrava",
        SchoolType.SecondarySchool,
        SchoolKind.Specialized,
        "600003003",
        "secondary.ostrava@skolio.local",
        "+420591000301",
        "https://secondary.skolio.local",
        Address.Create("Akademicka 5", "Ostrava", "70200", "CZ"),
        "Main campus + technology center",
        new DateOnly(2015, 9, 1),
        new DateOnly(2016, 9, 1),
        680,
        "cs",
        SecondaryOperator.Id,
        SecondaryFounder.Id,
        PlatformStatus.Active);

    private static readonly SchoolYearDefinition[] SchoolYears =
    [
        new(Guid.Parse("20000000-0000-0000-0000-000000000101"), KindergartenSchool.Id, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30)),
        new(Guid.Parse("20000000-0000-0000-0000-000000000201"), ElementarySchool.Id, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30)),
        new(Guid.Parse("20000000-0000-0000-0000-000000000301"), SecondarySchool.Id, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30))
    ];

    private static readonly GradeLevelDefinition[] GradeLevels =
    [
        new(Guid.Parse("21000000-0000-0000-0000-000000000201"), ElementarySchool.Id, SchoolType.ElementarySchool, 1, "1. rocnik"),
        new(Guid.Parse("21000000-0000-0000-0000-000000000301"), SecondarySchool.Id, SchoolType.SecondarySchool, 1, "1. rocnik")
    ];

    private static readonly ClassRoomDefinition[] ClassRooms =
    [
        new(Guid.Parse("22000000-0000-0000-0000-000000000201"), ElementarySchool.Id, GradeLevels[0].Id, SchoolType.ElementarySchool, "1A", "Trida 1A"),
        new(Guid.Parse("22000000-0000-0000-0000-000000000301"), SecondarySchool.Id, GradeLevels[1].Id, SchoolType.SecondarySchool, "S1A", "Trida S1A")
    ];

    private static readonly TeachingGroupDefinition[] TeachingGroups =
    [
        new(Guid.Parse("23000000-0000-0000-0000-000000000101"), KindergartenSchool.Id, null, "Berusky", true),
        new(Guid.Parse("23000000-0000-0000-0000-000000000201"), ElementarySchool.Id, ClassRooms[0].Id, "Skupina 1A", false),
        new(Guid.Parse("23000000-0000-0000-0000-000000000301"), SecondarySchool.Id, ClassRooms[1].Id, "Skupina S1A", false)
    ];

    private static readonly SubjectDefinition[] Subjects =
    [
        new(Guid.Parse("24000000-0000-0000-0000-000000000201"), ElementarySchool.Id, "MAT", "Matematika"),
        new(Guid.Parse("24000000-0000-0000-0000-000000000301"), SecondarySchool.Id, "INF", "Informatika")
    ];

    private static readonly SecondaryFieldOfStudyDefinition[] SecondaryFields =
    [
        new(Guid.Parse("25000000-0000-0000-0000-000000000301"), SecondarySchool.Id, SchoolType.SecondarySchool, "IT", "Informacni technologie")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Organization:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Organization seed is disabled by configuration.");
            return;
        }

        logger.LogInformation("Organization structural seed started.");

        var state = await EvaluateSeedStateAsync(cancellationToken);
        if (state == SeedState.FullyInitialized)
        {
            logger.LogInformation("Organization structural seed skipped: baseline already exists.");
            return;
        }

        if (state == SeedState.Inconsistent)
        {
            throw new InvalidOperationException("Organization seed aborted because critical inconsistencies were detected.");
        }

        await EnsureSchoolOperatorsAsync(cancellationToken);
        await EnsureFoundersAsync(cancellationToken);
        await EnsureSchoolsAsync(cancellationToken);
        await EnsureSchoolYearsAsync(cancellationToken);
        await EnsureGradeLevelsAsync(cancellationToken);
        await EnsureClassRoomsAsync(cancellationToken);
        await EnsureTeachingGroupsAsync(cancellationToken);
        await EnsureSubjectsAsync(cancellationToken);
        await EnsureSecondaryFieldsAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await ValidateConsistencyAsync(cancellationToken);
        logger.LogInformation("Organization structural seed completed.");
    }

    private async Task<SeedState> EvaluateSeedStateAsync(CancellationToken cancellationToken)
    {
        var hasSchools = await dbContext.Schools.AnyAsync(cancellationToken);
        var hasSchoolYears = await dbContext.SchoolYears.AnyAsync(cancellationToken);
        var hasGroups = await dbContext.TeachingGroups.AnyAsync(cancellationToken);

        var requiredSchools = new[] { KindergartenSchool.Id, ElementarySchool.Id, SecondarySchool.Id };
        var existingRequiredSchools = await dbContext.Schools.CountAsync(x => requiredSchools.Contains(x.Id), cancellationToken);

        if (!hasSchools && !hasSchoolYears && !hasGroups)
        {
            logger.LogInformation("Organization seed state detected as Empty.");
            return SeedState.Empty;
        }

        if (existingRequiredSchools == requiredSchools.Length)
        {
            await ValidateConsistencyAsync(cancellationToken);
            logger.LogInformation("Organization seed state detected as FullyInitialized.");
            return SeedState.FullyInitialized;
        }

        if (existingRequiredSchools > 0)
        {
            logger.LogWarning("Organization seed state detected as PartiallyInitialized. Missing baseline entities will be created.");
            return SeedState.PartiallyInitialized;
        }

        logger.LogError("Organization seed state detected as Inconsistent: existing data does not contain mandatory school baseline IDs.");
        return SeedState.Inconsistent;
    }

    private async Task EnsureSchoolOperatorsAsync(CancellationToken cancellationToken)
    {
        await EnsureSchoolOperatorAsync(KindergartenOperator, cancellationToken);
        await EnsureSchoolOperatorAsync(ElementaryOperator, cancellationToken);
        await EnsureSchoolOperatorAsync(SecondaryOperator, cancellationToken);
    }

    private async Task EnsureSchoolOperatorAsync(SchoolOperatorDefinition definition, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolOperators.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.SchoolOperators.Add(SchoolOperator.Create(
                definition.Id,
                definition.LegalEntityName,
                definition.LegalForm,
                definition.CompanyNumberIco,
                definition.RegisteredOfficeAddress,
                definition.ResortIdentifier,
                definition.DirectorSummary,
                definition.StatutoryBodySummary));
            logger.LogInformation("Organization seed created school operator {Name}.", definition.LegalEntityName);
            return;
        }

        if (!string.Equals(entity.LegalEntityName, definition.LegalEntityName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Seed inconsistency: school operator '{definition.Id}' has unexpected legal entity name '{entity.LegalEntityName}'.");
        }
    }

    private async Task EnsureFoundersAsync(CancellationToken cancellationToken)
    {
        await EnsureFounderAsync(KindergartenFounder, cancellationToken);
        await EnsureFounderAsync(ElementaryFounder, cancellationToken);
        await EnsureFounderAsync(SecondaryFounder, cancellationToken);
    }

    private async Task EnsureFounderAsync(FounderDefinition definition, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Founders.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.Founders.Add(Founder.Create(
                definition.Id,
                definition.FounderType,
                definition.FounderCategory,
                definition.FounderName,
                definition.FounderLegalForm,
                definition.FounderIco,
                definition.FounderAddress,
                definition.FounderEmail));
            logger.LogInformation("Organization seed created founder {Name}.", definition.FounderName);
            return;
        }

        if (!string.Equals(entity.FounderName, definition.FounderName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Seed inconsistency: founder '{definition.Id}' has unexpected name '{entity.FounderName}'.");
        }
    }

    private async Task EnsureSchoolsAsync(CancellationToken cancellationToken)
    {
        await EnsureSchoolAsync(KindergartenSchool, cancellationToken);
        await EnsureSchoolAsync(ElementarySchool, cancellationToken);
        await EnsureSchoolAsync(SecondarySchool, cancellationToken);
    }

    private async Task EnsureSchoolAsync(SchoolDefinition definition, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
        if (entity is null)
        {
            dbContext.Schools.Add(School.Create(
                definition.Id,
                definition.Name,
                definition.SchoolType,
                definition.SchoolKind,
                definition.SchoolIzo,
                definition.SchoolEmail,
                definition.SchoolPhone,
                definition.SchoolWebsite,
                definition.MainAddress,
                definition.EducationLocationsSummary,
                definition.RegistryEntryDate,
                definition.EducationStartDate,
                definition.MaxStudentCapacity,
                definition.TeachingLanguage,
                definition.SchoolOperatorId,
                definition.FounderId,
                definition.PlatformStatus));
            logger.LogInformation("Organization seed created school {Name}.", definition.Name);
            return;
        }

        if (!string.Equals(entity.Name, definition.Name, StringComparison.Ordinal) || entity.SchoolType != definition.SchoolType)
        {
            throw new InvalidOperationException($"Seed inconsistency: school '{definition.Id}' does not match mandatory baseline definition.");
        }
    }

    private async Task EnsureSchoolYearsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in SchoolYears)
        {
            var entity = await dbContext.SchoolYears.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.SchoolYears.Add(SchoolYear.Create(definition.Id, definition.SchoolId, definition.Label, definition.StartDate, definition.EndDate));
                logger.LogInformation("Organization seed created school year {Label} for school {SchoolId}.", definition.Label, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || !string.Equals(entity.Label, definition.Label, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Seed inconsistency: school year '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task EnsureGradeLevelsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in GradeLevels)
        {
            var entity = await dbContext.GradeLevels.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.GradeLevels.Add(GradeLevel.Create(definition.Id, definition.SchoolId, definition.SchoolType, definition.Level, definition.DisplayName));
                logger.LogInformation("Organization seed created grade level {DisplayName} for school {SchoolId}.", definition.DisplayName, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || entity.Level != definition.Level)
            {
                throw new InvalidOperationException($"Seed inconsistency: grade level '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task EnsureClassRoomsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in ClassRooms)
        {
            var entity = await dbContext.ClassRooms.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.ClassRooms.Add(ClassRoom.Create(definition.Id, definition.SchoolId, definition.GradeLevelId, definition.SchoolType, definition.Code, definition.DisplayName));
                logger.LogInformation("Organization seed created class room {Code} for school {SchoolId}.", definition.Code, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || entity.GradeLevelId != definition.GradeLevelId)
            {
                throw new InvalidOperationException($"Seed inconsistency: class room '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task EnsureTeachingGroupsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in TeachingGroups)
        {
            var entity = await dbContext.TeachingGroups.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.TeachingGroups.Add(TeachingGroup.Create(definition.Id, definition.SchoolId, definition.ClassRoomId, definition.Name, definition.IsDailyOperationsGroup));
                logger.LogInformation("Organization seed created group {Name} for school {SchoolId}.", definition.Name, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || entity.ClassRoomId != definition.ClassRoomId)
            {
                throw new InvalidOperationException($"Seed inconsistency: group '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task EnsureSubjectsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in Subjects)
        {
            var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.Subjects.Add(Subject.Create(definition.Id, definition.SchoolId, definition.Code, definition.Name));
                logger.LogInformation("Organization seed created subject {Code} for school {SchoolId}.", definition.Code, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || !string.Equals(entity.Code, definition.Code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Seed inconsistency: subject '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task EnsureSecondaryFieldsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in SecondaryFields)
        {
            var entity = await dbContext.SecondaryFieldsOfStudy.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.SecondaryFieldsOfStudy.Add(SecondaryFieldOfStudy.Create(definition.Id, definition.SchoolId, definition.SchoolType, definition.Code, definition.Name));
                logger.LogInformation("Organization seed created field of study {Code} for school {SchoolId}.", definition.Code, definition.SchoolId);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId || !string.Equals(entity.Code, definition.Code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Seed inconsistency: field of study '{definition.Id}' does not match mandatory baseline definition.");
            }
        }
    }

    private async Task ValidateConsistencyAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in SchoolYears)
        {
            var schoolExists = await dbContext.Schools.AnyAsync(x => x.Id == definition.SchoolId, cancellationToken);
            if (!schoolExists)
            {
                throw new InvalidOperationException($"Seed consistency validation failed: school year '{definition.Id}' references missing school '{definition.SchoolId}'.");
            }
        }

        foreach (var definition in ClassRooms)
        {
            var gradeLevelExists = await dbContext.GradeLevels.AnyAsync(x => x.Id == definition.GradeLevelId, cancellationToken);
            if (!gradeLevelExists)
            {
                throw new InvalidOperationException($"Seed consistency validation failed: class room '{definition.Id}' references missing grade level '{definition.GradeLevelId}'.");
            }
        }

        foreach (var definition in TeachingGroups.Where(x => x.ClassRoomId.HasValue))
        {
            var classExists = await dbContext.ClassRooms.AnyAsync(x => x.Id == definition.ClassRoomId, cancellationToken);
            if (!classExists)
            {
                throw new InvalidOperationException($"Seed consistency validation failed: group '{definition.Id}' references missing class room '{definition.ClassRoomId}'.");
            }
        }

        logger.LogInformation("Organization seed consistency validation completed successfully.");
    }

    private sealed record SchoolOperatorDefinition(Guid Id, string LegalEntityName, LegalForm LegalForm, string? CompanyNumberIco, Address RegisteredOfficeAddress, string? ResortIdentifier, string? DirectorSummary, string? StatutoryBodySummary);
    private sealed record FounderDefinition(Guid Id, FounderType FounderType, FounderCategory FounderCategory, string FounderName, LegalForm FounderLegalForm, string? FounderIco, Address FounderAddress, string? FounderEmail);
    private sealed record SchoolDefinition(Guid Id, string Name, SchoolType SchoolType, SchoolKind SchoolKind, string? SchoolIzo, string? SchoolEmail, string? SchoolPhone, string? SchoolWebsite, Address MainAddress, string? EducationLocationsSummary, DateOnly? RegistryEntryDate, DateOnly? EducationStartDate, int? MaxStudentCapacity, string? TeachingLanguage, Guid SchoolOperatorId, Guid FounderId, PlatformStatus PlatformStatus);
    private sealed record SchoolYearDefinition(Guid Id, Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
    private sealed record GradeLevelDefinition(Guid Id, Guid SchoolId, SchoolType SchoolType, int Level, string DisplayName);
    private sealed record ClassRoomDefinition(Guid Id, Guid SchoolId, Guid GradeLevelId, SchoolType SchoolType, string Code, string DisplayName);
    private sealed record TeachingGroupDefinition(Guid Id, Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
    private sealed record SubjectDefinition(Guid Id, Guid SchoolId, string Code, string Name);
    private sealed record SecondaryFieldOfStudyDefinition(Guid Id, Guid SchoolId, SchoolType SchoolType, string Code, string Name);

    private enum SeedState
    {
        Empty,
        PartiallyInitialized,
        FullyInitialized,
        Inconsistent
    }
}
