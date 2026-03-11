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
        null,
        Address.Create("Skolkova 1", "Brno", "60200", "CZ"),
        "operator.kg@skolio.local",
        null,
        "RZ-KG-001",
        "Jana Novakova",
        "Jednatel");

    private static readonly SchoolOperatorDefinition ElementaryOperator = new(
        Guid.Parse("42222222-2222-2222-2222-222222222222"),
        "Skolio Elementary Operations a.s.",
        LegalForm.JointStockCompany,
        "22345671",
        null,
        Address.Create("Skolni 10", "Praha", "11000", "CZ"),
        "operator.el@skolio.local",
        null,
        "RZ-EL-001",
        "Petr Svoboda",
        "Predstavenstvo");

    private static readonly SchoolOperatorDefinition SecondaryOperator = new(
        Guid.Parse("43333333-3333-3333-3333-333333333333"),
        "Skolio Secondary Operations z.u.",
        LegalForm.NonProfitOrganization,
        "32345672",
        null,
        Address.Create("Akademicka 5", "Ostrava", "70200", "CZ"),
        "operator.se@skolio.local",
        null,
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
        "podatelna@brno.cz",
        null);

    private static readonly FounderDefinition ElementaryFounder = new(
        Guid.Parse("52222222-2222-2222-2222-222222222222"),
        FounderType.Region,
        FounderCategory.Public,
        "Hlavni mesto Praha",
        LegalForm.Region,
        "00064581",
        Address.Create("Marianske namesti 2", "Praha", "11000", "CZ"),
        "posta@praha.eu",
        null);

    private static readonly FounderDefinition SecondaryFounder = new(
        Guid.Parse("53333333-3333-3333-3333-333333333333"),
        FounderType.LegalEntity,
        FounderCategory.Private,
        "Skolio Education Foundation",
        LegalForm.NonProfitOrganization,
        "27345673",
        Address.Create("Nadrazni 45", "Ostrava", "70200", "CZ"),
        "contact@skolio.foundation",
        null);

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

    // School Context Scope Matrix IDs — stable seed identifiers, one per SchoolType
    private static readonly Guid MatrixKindergartenId = Guid.Parse("a0000001-0000-0000-0000-000000000001");
    private static readonly Guid MatrixElementaryId = Guid.Parse("a0000002-0000-0000-0000-000000000002");
    private static readonly Guid MatrixSecondaryId = Guid.Parse("a0000003-0000-0000-0000-000000000003");

    private static readonly ScopeMatrixDefinition[] ScopeMatrices =
    [
        new(MatrixKindergartenId, SchoolType.Kindergarten, "KindergartenDefault", "school_context.matrix.kindergarten"),
        new(MatrixElementaryId, SchoolType.ElementarySchool, "ElementarySchoolDefault", "school_context.matrix.elementary"),
        new(MatrixSecondaryId, SchoolType.SecondarySchool, "SecondarySchoolDefault", "school_context.matrix.secondary")
    ];

    // Capabilities: UsesClasses(1), UsesGroups(2), UsesSubjects(3), UsesFieldOfStudy(4),
    //               UsesDailyReports(5), UsesAttendance(6), UsesGrades(7), UsesHomework(8)
    private static readonly CapabilityDefinition[] Capabilities =
    [
        // Kindergarten: group-centric, daily reports, attendance only
        new(Guid.Parse("b1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, ScopeCapabilityCode.UsesClasses, false, "scope.capability.uses_classes"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, ScopeCapabilityCode.UsesGroups, true, "scope.capability.uses_groups"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, ScopeCapabilityCode.UsesSubjects, false, "scope.capability.uses_subjects"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, ScopeCapabilityCode.UsesFieldOfStudy, false, "scope.capability.uses_field_of_study"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000005"), MatrixKindergartenId, ScopeCapabilityCode.UsesDailyReports, true, "scope.capability.uses_daily_reports"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000006"), MatrixKindergartenId, ScopeCapabilityCode.UsesAttendance, true, "scope.capability.uses_attendance"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000007"), MatrixKindergartenId, ScopeCapabilityCode.UsesGrades, false, "scope.capability.uses_grades"),
        new(Guid.Parse("b1000001-0000-0000-0000-000000000008"), MatrixKindergartenId, ScopeCapabilityCode.UsesHomework, false, "scope.capability.uses_homework"),

        // ElementarySchool: class + subject centric, attendance + grades + homework
        new(Guid.Parse("b2000002-0000-0000-0000-000000000001"), MatrixElementaryId, ScopeCapabilityCode.UsesClasses, true, "scope.capability.uses_classes"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000002"), MatrixElementaryId, ScopeCapabilityCode.UsesGroups, true, "scope.capability.uses_groups"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000003"), MatrixElementaryId, ScopeCapabilityCode.UsesSubjects, true, "scope.capability.uses_subjects"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000004"), MatrixElementaryId, ScopeCapabilityCode.UsesFieldOfStudy, false, "scope.capability.uses_field_of_study"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000005"), MatrixElementaryId, ScopeCapabilityCode.UsesDailyReports, false, "scope.capability.uses_daily_reports"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000006"), MatrixElementaryId, ScopeCapabilityCode.UsesAttendance, true, "scope.capability.uses_attendance"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000007"), MatrixElementaryId, ScopeCapabilityCode.UsesGrades, true, "scope.capability.uses_grades"),
        new(Guid.Parse("b2000002-0000-0000-0000-000000000008"), MatrixElementaryId, ScopeCapabilityCode.UsesHomework, true, "scope.capability.uses_homework"),

        // SecondarySchool: class + subject + field-of-study centric
        new(Guid.Parse("b3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, ScopeCapabilityCode.UsesClasses, true, "scope.capability.uses_classes"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, ScopeCapabilityCode.UsesGroups, true, "scope.capability.uses_groups"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, ScopeCapabilityCode.UsesSubjects, true, "scope.capability.uses_subjects"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, ScopeCapabilityCode.UsesFieldOfStudy, true, "scope.capability.uses_field_of_study"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000005"), MatrixSecondaryId, ScopeCapabilityCode.UsesDailyReports, false, "scope.capability.uses_daily_reports"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000006"), MatrixSecondaryId, ScopeCapabilityCode.UsesAttendance, true, "scope.capability.uses_attendance"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000007"), MatrixSecondaryId, ScopeCapabilityCode.UsesGrades, true, "scope.capability.uses_grades"),
        new(Guid.Parse("b3000003-0000-0000-0000-000000000008"), MatrixSecondaryId, ScopeCapabilityCode.UsesHomework, true, "scope.capability.uses_homework")
    ];

    // Allowed roles — PlatformAdministrator is global, not school-scoped; matrix covers school-scoped roles only
    private static readonly AllowedRoleDefinition[] AllowedRoles =
    [
        new(Guid.Parse("c1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, "SchoolAdministrator", "role.school_administrator"),
        new(Guid.Parse("c1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, "Teacher", "role.teacher"),
        new(Guid.Parse("c1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, "Parent", "role.parent"),
        new(Guid.Parse("c1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, "Student", "role.student"),

        new(Guid.Parse("c2000002-0000-0000-0000-000000000001"), MatrixElementaryId, "SchoolAdministrator", "role.school_administrator"),
        new(Guid.Parse("c2000002-0000-0000-0000-000000000002"), MatrixElementaryId, "Teacher", "role.teacher"),
        new(Guid.Parse("c2000002-0000-0000-0000-000000000003"), MatrixElementaryId, "Parent", "role.parent"),
        new(Guid.Parse("c2000002-0000-0000-0000-000000000004"), MatrixElementaryId, "Student", "role.student"),

        new(Guid.Parse("c3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, "SchoolAdministrator", "role.school_administrator"),
        new(Guid.Parse("c3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, "Teacher", "role.teacher"),
        new(Guid.Parse("c3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, "Parent", "role.parent"),
        new(Guid.Parse("c3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, "Student", "role.student")
    ];

    // Allowed profile sections
    private static readonly AllowedProfileSectionDefinition[] AllowedProfileSections =
    [
        // Kindergarten: student health + legal guardians relevant, no qualifications
        new(Guid.Parse("d1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, ProfileSectionCode.BasicInfo, "profile.section.basic_info"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, ProfileSectionCode.ContactInfo, "profile.section.contact_info"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, ProfileSectionCode.Address, "profile.section.address"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, ProfileSectionCode.HealthAndSafety, "profile.section.health_and_safety"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000005"), MatrixKindergartenId, ProfileSectionCode.LegalGuardians, "profile.section.legal_guardians"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000006"), MatrixKindergartenId, ProfileSectionCode.SchoolPlacement, "profile.section.school_placement"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000007"), MatrixKindergartenId, ProfileSectionCode.SchoolContext, "profile.section.school_context"),
        new(Guid.Parse("d1000001-0000-0000-0000-000000000008"), MatrixKindergartenId, ProfileSectionCode.AdministratorContext, "profile.section.administrator_context"),

        // ElementarySchool: adds support measures and qualifications
        new(Guid.Parse("d2000002-0000-0000-0000-000000000001"), MatrixElementaryId, ProfileSectionCode.BasicInfo, "profile.section.basic_info"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000002"), MatrixElementaryId, ProfileSectionCode.ContactInfo, "profile.section.contact_info"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000003"), MatrixElementaryId, ProfileSectionCode.Address, "profile.section.address"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000004"), MatrixElementaryId, ProfileSectionCode.HealthAndSafety, "profile.section.health_and_safety"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000005"), MatrixElementaryId, ProfileSectionCode.LegalGuardians, "profile.section.legal_guardians"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000006"), MatrixElementaryId, ProfileSectionCode.SchoolPlacement, "profile.section.school_placement"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000007"), MatrixElementaryId, ProfileSectionCode.SupportMeasures, "profile.section.support_measures"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000008"), MatrixElementaryId, ProfileSectionCode.Qualifications, "profile.section.qualifications"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000009"), MatrixElementaryId, ProfileSectionCode.SchoolContext, "profile.section.school_context"),
        new(Guid.Parse("d2000002-0000-0000-0000-000000000010"), MatrixElementaryId, ProfileSectionCode.AdministratorContext, "profile.section.administrator_context"),

        // SecondarySchool: all sections
        new(Guid.Parse("d3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, ProfileSectionCode.BasicInfo, "profile.section.basic_info"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, ProfileSectionCode.ContactInfo, "profile.section.contact_info"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, ProfileSectionCode.Address, "profile.section.address"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, ProfileSectionCode.HealthAndSafety, "profile.section.health_and_safety"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000005"), MatrixSecondaryId, ProfileSectionCode.LegalGuardians, "profile.section.legal_guardians"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000006"), MatrixSecondaryId, ProfileSectionCode.SchoolPlacement, "profile.section.school_placement"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000007"), MatrixSecondaryId, ProfileSectionCode.SupportMeasures, "profile.section.support_measures"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000008"), MatrixSecondaryId, ProfileSectionCode.Qualifications, "profile.section.qualifications"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000009"), MatrixSecondaryId, ProfileSectionCode.SchoolContext, "profile.section.school_context"),
        new(Guid.Parse("d3000003-0000-0000-0000-000000000010"), MatrixSecondaryId, ProfileSectionCode.AdministratorContext, "profile.section.administrator_context")
    ];

    // Allowed create-user flows — all types support all 4 flows
    private static readonly AllowedCreateUserFlowDefinition[] AllowedCreateUserFlows =
    [
        new(Guid.Parse("e1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, CreateUserFlowCode.CreateStudent, "flow.create.student"),
        new(Guid.Parse("e1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, CreateUserFlowCode.CreateParent, "flow.create.parent"),
        new(Guid.Parse("e1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, CreateUserFlowCode.CreateTeacher, "flow.create.teacher"),
        new(Guid.Parse("e1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, CreateUserFlowCode.CreateSchoolAdministrator, "flow.create.school_administrator"),

        new(Guid.Parse("e2000002-0000-0000-0000-000000000001"), MatrixElementaryId, CreateUserFlowCode.CreateStudent, "flow.create.student"),
        new(Guid.Parse("e2000002-0000-0000-0000-000000000002"), MatrixElementaryId, CreateUserFlowCode.CreateParent, "flow.create.parent"),
        new(Guid.Parse("e2000002-0000-0000-0000-000000000003"), MatrixElementaryId, CreateUserFlowCode.CreateTeacher, "flow.create.teacher"),
        new(Guid.Parse("e2000002-0000-0000-0000-000000000004"), MatrixElementaryId, CreateUserFlowCode.CreateSchoolAdministrator, "flow.create.school_administrator"),

        new(Guid.Parse("e3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, CreateUserFlowCode.CreateStudent, "flow.create.student"),
        new(Guid.Parse("e3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, CreateUserFlowCode.CreateParent, "flow.create.parent"),
        new(Guid.Parse("e3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, CreateUserFlowCode.CreateTeacher, "flow.create.teacher"),
        new(Guid.Parse("e3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, CreateUserFlowCode.CreateSchoolAdministrator, "flow.create.school_administrator")
    ];

    // Allowed user management flows — all types support all 6 flows
    private static readonly AllowedUserManagementFlowDefinition[] AllowedUserManagementFlows =
    [
        new(Guid.Parse("f1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, UserManagementFlowCode.EditStudent, "flow.manage.edit_student"),
        new(Guid.Parse("f1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, UserManagementFlowCode.EditParent, "flow.manage.edit_parent"),
        new(Guid.Parse("f1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, UserManagementFlowCode.EditTeacher, "flow.manage.edit_teacher"),
        new(Guid.Parse("f1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, UserManagementFlowCode.EditSchoolAdministrator, "flow.manage.edit_school_administrator"),
        new(Guid.Parse("f1000001-0000-0000-0000-000000000005"), MatrixKindergartenId, UserManagementFlowCode.DeactivateUser, "flow.manage.deactivate_user"),
        new(Guid.Parse("f1000001-0000-0000-0000-000000000006"), MatrixKindergartenId, UserManagementFlowCode.ReactivateUser, "flow.manage.reactivate_user"),

        new(Guid.Parse("f2000002-0000-0000-0000-000000000001"), MatrixElementaryId, UserManagementFlowCode.EditStudent, "flow.manage.edit_student"),
        new(Guid.Parse("f2000002-0000-0000-0000-000000000002"), MatrixElementaryId, UserManagementFlowCode.EditParent, "flow.manage.edit_parent"),
        new(Guid.Parse("f2000002-0000-0000-0000-000000000003"), MatrixElementaryId, UserManagementFlowCode.EditTeacher, "flow.manage.edit_teacher"),
        new(Guid.Parse("f2000002-0000-0000-0000-000000000004"), MatrixElementaryId, UserManagementFlowCode.EditSchoolAdministrator, "flow.manage.edit_school_administrator"),
        new(Guid.Parse("f2000002-0000-0000-0000-000000000005"), MatrixElementaryId, UserManagementFlowCode.DeactivateUser, "flow.manage.deactivate_user"),
        new(Guid.Parse("f2000002-0000-0000-0000-000000000006"), MatrixElementaryId, UserManagementFlowCode.ReactivateUser, "flow.manage.reactivate_user"),

        new(Guid.Parse("f3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, UserManagementFlowCode.EditStudent, "flow.manage.edit_student"),
        new(Guid.Parse("f3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, UserManagementFlowCode.EditParent, "flow.manage.edit_parent"),
        new(Guid.Parse("f3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, UserManagementFlowCode.EditTeacher, "flow.manage.edit_teacher"),
        new(Guid.Parse("f3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, UserManagementFlowCode.EditSchoolAdministrator, "flow.manage.edit_school_administrator"),
        new(Guid.Parse("f3000003-0000-0000-0000-000000000005"), MatrixSecondaryId, UserManagementFlowCode.DeactivateUser, "flow.manage.deactivate_user"),
        new(Guid.Parse("f3000003-0000-0000-0000-000000000006"), MatrixSecondaryId, UserManagementFlowCode.ReactivateUser, "flow.manage.reactivate_user")
    ];

    // Allowed organization sections
    private static readonly AllowedOrganizationSectionDefinition[] AllowedOrganizationSections =
    [
        // Kindergarten: no classes, no subjects, no fields of study, no grade levels
        new(Guid.Parse("g1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, OrganizationSectionCode.SchoolInfo, "org.section.school_info"),
        new(Guid.Parse("g1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, OrganizationSectionCode.SchoolOperator, "org.section.school_operator"),
        new(Guid.Parse("g1000001-0000-0000-0000-000000000003"), MatrixKindergartenId, OrganizationSectionCode.Founder, "org.section.founder"),
        new(Guid.Parse("g1000001-0000-0000-0000-000000000004"), MatrixKindergartenId, OrganizationSectionCode.SchoolYears, "org.section.school_years"),
        new(Guid.Parse("g1000001-0000-0000-0000-000000000005"), MatrixKindergartenId, OrganizationSectionCode.Groups, "org.section.groups"),

        // ElementarySchool: adds classes, grade levels, subjects; no fields of study
        new(Guid.Parse("g2000002-0000-0000-0000-000000000001"), MatrixElementaryId, OrganizationSectionCode.SchoolInfo, "org.section.school_info"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000002"), MatrixElementaryId, OrganizationSectionCode.SchoolOperator, "org.section.school_operator"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000003"), MatrixElementaryId, OrganizationSectionCode.Founder, "org.section.founder"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000004"), MatrixElementaryId, OrganizationSectionCode.SchoolYears, "org.section.school_years"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000005"), MatrixElementaryId, OrganizationSectionCode.GradeLevels, "org.section.grade_levels"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000006"), MatrixElementaryId, OrganizationSectionCode.Classes, "org.section.classes"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000007"), MatrixElementaryId, OrganizationSectionCode.Groups, "org.section.groups"),
        new(Guid.Parse("g2000002-0000-0000-0000-000000000008"), MatrixElementaryId, OrganizationSectionCode.Subjects, "org.section.subjects"),

        // SecondarySchool: all sections including fields of study
        new(Guid.Parse("g3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, OrganizationSectionCode.SchoolInfo, "org.section.school_info"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, OrganizationSectionCode.SchoolOperator, "org.section.school_operator"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, OrganizationSectionCode.Founder, "org.section.founder"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000004"), MatrixSecondaryId, OrganizationSectionCode.SchoolYears, "org.section.school_years"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000005"), MatrixSecondaryId, OrganizationSectionCode.GradeLevels, "org.section.grade_levels"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000006"), MatrixSecondaryId, OrganizationSectionCode.Classes, "org.section.classes"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000007"), MatrixSecondaryId, OrganizationSectionCode.Groups, "org.section.groups"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000008"), MatrixSecondaryId, OrganizationSectionCode.Subjects, "org.section.subjects"),
        new(Guid.Parse("g3000003-0000-0000-0000-000000000009"), MatrixSecondaryId, OrganizationSectionCode.FieldsOfStudy, "org.section.fields_of_study")
    ];

    // Allowed academics sections
    private static readonly AllowedAcademicsSectionDefinition[] AllowedAcademicsSections =
    [
        // Kindergarten: daily reports + attendance
        new(Guid.Parse("h1000001-0000-0000-0000-000000000001"), MatrixKindergartenId, AcademicsSectionCode.DailyReports, "academics.section.daily_reports"),
        new(Guid.Parse("h1000001-0000-0000-0000-000000000002"), MatrixKindergartenId, AcademicsSectionCode.Attendance, "academics.section.attendance"),

        // ElementarySchool: attendance + grades + homework
        new(Guid.Parse("h2000002-0000-0000-0000-000000000001"), MatrixElementaryId, AcademicsSectionCode.Attendance, "academics.section.attendance"),
        new(Guid.Parse("h2000002-0000-0000-0000-000000000002"), MatrixElementaryId, AcademicsSectionCode.Grades, "academics.section.grades"),
        new(Guid.Parse("h2000002-0000-0000-0000-000000000003"), MatrixElementaryId, AcademicsSectionCode.Homework, "academics.section.homework"),

        // SecondarySchool: attendance + grades + homework
        new(Guid.Parse("h3000003-0000-0000-0000-000000000001"), MatrixSecondaryId, AcademicsSectionCode.Attendance, "academics.section.attendance"),
        new(Guid.Parse("h3000003-0000-0000-0000-000000000002"), MatrixSecondaryId, AcademicsSectionCode.Grades, "academics.section.grades"),
        new(Guid.Parse("h3000003-0000-0000-0000-000000000003"), MatrixSecondaryId, AcademicsSectionCode.Homework, "academics.section.homework")
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
        await EnsureSchoolContextMatricesAsync(cancellationToken);

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

        var requiredMatrices = new[] { MatrixKindergartenId, MatrixElementaryId, MatrixSecondaryId };
        var existingRequiredMatrices = await dbContext.SchoolContextScopeMatrices.CountAsync(x => requiredMatrices.Contains(x.Id), cancellationToken);

        if (!hasSchools && !hasSchoolYears && !hasGroups)
        {
            logger.LogInformation("Organization seed state detected as Empty.");
            return SeedState.Empty;
        }

        if (existingRequiredSchools == requiredSchools.Length && existingRequiredMatrices == requiredMatrices.Length)
        {
            await ValidateConsistencyAsync(cancellationToken);
            logger.LogInformation("Organization seed state detected as FullyInitialized.");
            return SeedState.FullyInitialized;
        }

        if (existingRequiredSchools > 0 || existingRequiredMatrices > 0)
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
                definition.RedIzo,
                definition.RegisteredOfficeAddress,
                definition.OperatorEmail,
                definition.DataBox,
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
                definition.FounderEmail,
                definition.FounderDataBox));
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

    private async Task EnsureSchoolContextMatricesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in ScopeMatrices)
        {
            var entity = await dbContext.SchoolContextScopeMatrices.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                dbContext.SchoolContextScopeMatrices.Add(SchoolContextScopeMatrix.Create(
                    definition.Id,
                    definition.SchoolType,
                    definition.Code,
                    definition.TranslationKey));
                logger.LogInformation("Organization seed created scope matrix {Code} for school type {SchoolType}.", definition.Code, definition.SchoolType);
            }
            else if (entity.SchoolType != definition.SchoolType || !string.Equals(entity.Code, definition.Code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Seed inconsistency: scope matrix '{definition.Id}' does not match mandatory baseline definition.");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureCapabilitiesAsync(cancellationToken);
        await EnsureAllowedRolesAsync(cancellationToken);
        await EnsureAllowedProfileSectionsAsync(cancellationToken);
        await EnsureAllowedCreateUserFlowsAsync(cancellationToken);
        await EnsureAllowedUserManagementFlowsAsync(cancellationToken);
        await EnsureAllowedOrganizationSectionsAsync(cancellationToken);
        await EnsureAllowedAcademicsSectionsAsync(cancellationToken);
    }

    private async Task EnsureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in Capabilities)
        {
            var exists = await dbContext.SchoolContextScopeCapabilities.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeCapabilities.Add(SchoolContextScopeCapability.Create(
                    definition.Id, definition.MatrixId, definition.CapabilityCode, definition.TranslationKey, definition.IsEnabled));
                logger.LogInformation("Organization seed created capability {Code} for matrix {MatrixId}.", definition.CapabilityCode, definition.MatrixId);
            }
        }
    }

    private async Task EnsureAllowedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedRoles)
        {
            var exists = await dbContext.SchoolContextScopeAllowedRoles.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedRoles.Add(SchoolContextScopeAllowedRole.Create(
                    definition.Id, definition.MatrixId, definition.RoleCode, definition.TranslationKey));
                logger.LogInformation("Organization seed created allowed role {Role} for matrix {MatrixId}.", definition.RoleCode, definition.MatrixId);
            }
        }
    }

    private async Task EnsureAllowedProfileSectionsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedProfileSections)
        {
            var exists = await dbContext.SchoolContextScopeAllowedProfileSections.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedProfileSections.Add(SchoolContextScopeAllowedProfileSection.Create(
                    definition.Id, definition.MatrixId, definition.SectionCode, definition.TranslationKey));
            }
        }
    }

    private async Task EnsureAllowedCreateUserFlowsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedCreateUserFlows)
        {
            var exists = await dbContext.SchoolContextScopeAllowedCreateUserFlows.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedCreateUserFlows.Add(SchoolContextScopeAllowedCreateUserFlow.Create(
                    definition.Id, definition.MatrixId, definition.FlowCode, definition.TranslationKey));
            }
        }
    }

    private async Task EnsureAllowedUserManagementFlowsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedUserManagementFlows)
        {
            var exists = await dbContext.SchoolContextScopeAllowedUserManagementFlows.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedUserManagementFlows.Add(SchoolContextScopeAllowedUserManagementFlow.Create(
                    definition.Id, definition.MatrixId, definition.FlowCode, definition.TranslationKey));
            }
        }
    }

    private async Task EnsureAllowedOrganizationSectionsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedOrganizationSections)
        {
            var exists = await dbContext.SchoolContextScopeAllowedOrganizationSections.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedOrganizationSections.Add(SchoolContextScopeAllowedOrganizationSection.Create(
                    definition.Id, definition.MatrixId, definition.SectionCode, definition.TranslationKey));
            }
        }
    }

    private async Task EnsureAllowedAcademicsSectionsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in AllowedAcademicsSections)
        {
            var exists = await dbContext.SchoolContextScopeAllowedAcademicsSections.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!exists)
            {
                dbContext.SchoolContextScopeAllowedAcademicsSections.Add(SchoolContextScopeAllowedAcademicsSection.Create(
                    definition.Id, definition.MatrixId, definition.SectionCode, definition.TranslationKey));
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

        foreach (var definition in ScopeMatrices)
        {
            var matrixExists = await dbContext.SchoolContextScopeMatrices.AnyAsync(x => x.Id == definition.Id, cancellationToken);
            if (!matrixExists)
            {
                throw new InvalidOperationException($"Seed consistency validation failed: scope matrix '{definition.Id}' for school type '{definition.SchoolType}' is missing.");
            }

            var capabilityCount = await dbContext.SchoolContextScopeCapabilities.CountAsync(x => x.MatrixId == definition.Id, cancellationToken);
            if (capabilityCount < 8)
            {
                throw new InvalidOperationException($"Seed consistency validation failed: scope matrix '{definition.Id}' has only {capabilityCount} capabilities, expected 8.");
            }
        }

        logger.LogInformation("Organization seed consistency validation completed successfully.");
    }

    private sealed record SchoolOperatorDefinition(Guid Id, string LegalEntityName, LegalForm LegalForm, string? CompanyNumberIco, string? RedIzo, Address RegisteredOfficeAddress, string? OperatorEmail, string? DataBox, string? ResortIdentifier, string? DirectorSummary, string? StatutoryBodySummary);
    private sealed record FounderDefinition(Guid Id, FounderType FounderType, FounderCategory FounderCategory, string FounderName, LegalForm FounderLegalForm, string? FounderIco, Address FounderAddress, string? FounderEmail, string? FounderDataBox);
    private sealed record SchoolDefinition(Guid Id, string Name, SchoolType SchoolType, SchoolKind SchoolKind, string? SchoolIzo, string? SchoolEmail, string? SchoolPhone, string? SchoolWebsite, Address MainAddress, string? EducationLocationsSummary, DateOnly? RegistryEntryDate, DateOnly? EducationStartDate, int? MaxStudentCapacity, string? TeachingLanguage, Guid SchoolOperatorId, Guid FounderId, PlatformStatus PlatformStatus);
    private sealed record SchoolYearDefinition(Guid Id, Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
    private sealed record GradeLevelDefinition(Guid Id, Guid SchoolId, SchoolType SchoolType, int Level, string DisplayName);
    private sealed record ClassRoomDefinition(Guid Id, Guid SchoolId, Guid GradeLevelId, SchoolType SchoolType, string Code, string DisplayName);
    private sealed record TeachingGroupDefinition(Guid Id, Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
    private sealed record SubjectDefinition(Guid Id, Guid SchoolId, string Code, string Name);
    private sealed record SecondaryFieldOfStudyDefinition(Guid Id, Guid SchoolId, SchoolType SchoolType, string Code, string Name);
    private sealed record ScopeMatrixDefinition(Guid Id, SchoolType SchoolType, string Code, string TranslationKey);
    private sealed record CapabilityDefinition(Guid Id, Guid MatrixId, ScopeCapabilityCode CapabilityCode, bool IsEnabled, string TranslationKey);
    private sealed record AllowedRoleDefinition(Guid Id, Guid MatrixId, string RoleCode, string TranslationKey);
    private sealed record AllowedProfileSectionDefinition(Guid Id, Guid MatrixId, ProfileSectionCode SectionCode, string TranslationKey);
    private sealed record AllowedCreateUserFlowDefinition(Guid Id, Guid MatrixId, CreateUserFlowCode FlowCode, string TranslationKey);
    private sealed record AllowedUserManagementFlowDefinition(Guid Id, Guid MatrixId, UserManagementFlowCode FlowCode, string TranslationKey);
    private sealed record AllowedOrganizationSectionDefinition(Guid Id, Guid MatrixId, OrganizationSectionCode SectionCode, string TranslationKey);
    private sealed record AllowedAcademicsSectionDefinition(Guid Id, Guid MatrixId, AcademicsSectionCode SectionCode, string TranslationKey);

    private enum SeedState
    {
        Empty,
        PartiallyInitialized,
        FullyInitialized,
        Inconsistent
    }
}
