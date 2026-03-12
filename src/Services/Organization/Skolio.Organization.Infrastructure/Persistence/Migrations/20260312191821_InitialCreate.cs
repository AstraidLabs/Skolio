using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "class_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "founders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    founder_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    founder_category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    founder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    founder_legal_form = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    founder_ico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    founder_address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    founder_address_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    founder_address_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    founder_address_country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    founder_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    founder_data_box = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_founders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grade_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grade_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scope_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_bootstrap_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_create_user_flow_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_user_management_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secondary_fields_of_study",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secondary_fields_of_study", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_matrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_type_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_matrices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_entity_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_form = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    company_number_ico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    red_izo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    registered_office_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registered_office_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    registered_office_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    registered_office_country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    operator_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    data_box = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    resort_identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    director_summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    statutory_body_summary = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_operators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teacher_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    class_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teaching_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teaching_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_daily_operations_group = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teaching_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_academic_structure_matrix_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_scope_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    uses_subjects = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    uses_field_of_study = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    subject_is_class_bound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    field_of_study_is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_academic_structure_matrix_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_academic_structure_matrix_entries_school_conte~",
                        column: x => x.parent_scope_matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_assignment_matrix_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_scope_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    allows_class_room_assignment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allows_group_assignment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allows_subject_assignment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    student_requires_class_placement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    student_requires_group_placement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_assignment_matrix_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_assignment_matrix_entries_school_context_scope~",
                        column: x => x.parent_scope_matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_capacity_matrix_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_scope_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    capacity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_capacity_matrix_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_capacity_matrix_entries_school_context_scope_m~",
                        column: x => x.parent_scope_matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_registry_matrix_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_scope_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requires_izo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_red_izo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_ico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_data_box = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_founder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    requires_teaching_language = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_registry_matrix_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_registry_matrix_entries_school_context_scope_m~",
                        column: x => x.parent_scope_matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_school_structure_matrix_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_scope_matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    uses_grade_levels = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    uses_classes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    uses_groups = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    group_is_primary_structure = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_school_structure_matrix_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_school_structure_matrix_entries_school_context~",
                        column: x => x.parent_scope_matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_academics_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_academics_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_academics_sections_school_cont~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_create_user_flows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flow_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_create_user_flows", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_create_user_flows_school_conte~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_organization_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_organization_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_organization_sections_school_c~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_profile_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_profile_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_profile_sections_school_contex~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_roles_school_context_scope_mat~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_allowed_user_management_flows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flow_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_allowed_user_management_flows", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_allowed_user_management_flows_school_c~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_context_scope_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    translation_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_context_scope_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_context_scope_capabilities_school_context_scope_matr~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    school_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    school_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "General"),
                    school_izo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    school_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    school_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    school_website = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    main_address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    main_address_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    main_address_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    main_address_country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    education_locations_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    registry_entry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    education_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    max_student_capacity = table.Column<int>(type: "integer", nullable: true),
                    teaching_language = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    school_operator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    founder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    platform_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    school_administrator_user_profile_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schools", x => x.id);
                    table.ForeignKey(
                        name: "FK_schools_founders_founder_id",
                        column: x => x.founder_id,
                        principalTable: "founders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schools_school_operators_school_operator_id",
                        column: x => x.school_operator_id,
                        principalTable: "school_operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_capacities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    max_capacity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_capacities", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_capacities_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_places_of_education",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    address_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    address_country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_places_of_education", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_places_of_education_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_scope_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    override_uses_classes = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_groups = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_subjects = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_field_of_study = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_daily_reports = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_attendance = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_grades = table.Column<bool>(type: "boolean", nullable: true),
                    override_uses_homework = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_scope_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_scope_overrides_school_context_scope_matrices_matrix~",
                        column: x => x.matrix_id,
                        principalTable: "school_context_scope_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_scope_overrides_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_years",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_years", x => x.id);
                    table.ForeignKey(
                        name: "FK_school_years_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_founders_founder_category",
                table: "founders",
                column: "founder_category");

            migrationBuilder.CreateIndex(
                name: "IX_founders_founder_type",
                table: "founders",
                column: "founder_type");

            migrationBuilder.CreateIndex(
                name: "IX_organization_academic_structure_matrix_entries_code",
                table: "organization_academic_structure_matrix_entries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_academic_structure_matrix_entries_parent_scope~",
                table: "organization_academic_structure_matrix_entries",
                column: "parent_scope_matrix_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_assignment_matrix_entries_code",
                table: "organization_assignment_matrix_entries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_assignment_matrix_entries_parent_scope_matrix_~",
                table: "organization_assignment_matrix_entries",
                column: "parent_scope_matrix_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_capacity_matrix_entries_code",
                table: "organization_capacity_matrix_entries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_capacity_matrix_entries_parent_scope_matrix_id~",
                table: "organization_capacity_matrix_entries",
                columns: new[] { "parent_scope_matrix_id", "capacity_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_registry_matrix_entries_code",
                table: "organization_registry_matrix_entries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_registry_matrix_entries_parent_scope_matrix_id",
                table: "organization_registry_matrix_entries",
                column: "parent_scope_matrix_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_school_structure_matrix_entries_code",
                table: "organization_school_structure_matrix_entries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_school_structure_matrix_entries_parent_scope_m~",
                table: "organization_school_structure_matrix_entries",
                column: "parent_scope_matrix_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_definitions_role_code",
                table: "role_definitions",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_capacities_school_id",
                table: "school_capacities",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_capacities_school_id_capacity_type",
                table: "school_capacities",
                columns: new[] { "school_id", "capacity_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_academics_sections_matrix_id_s~",
                table: "school_context_scope_allowed_academics_sections",
                columns: new[] { "matrix_id", "section_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_create_user_flows_matrix_id_fl~",
                table: "school_context_scope_allowed_create_user_flows",
                columns: new[] { "matrix_id", "flow_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_organization_sections_matrix_i~",
                table: "school_context_scope_allowed_organization_sections",
                columns: new[] { "matrix_id", "section_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_profile_sections_matrix_id_sec~",
                table: "school_context_scope_allowed_profile_sections",
                columns: new[] { "matrix_id", "section_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_roles_matrix_id_role_code",
                table: "school_context_scope_allowed_roles",
                columns: new[] { "matrix_id", "role_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_user_management_flows_matrix_i~",
                table: "school_context_scope_allowed_user_management_flows",
                columns: new[] { "matrix_id", "flow_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_capabilities_matrix_id_capability_code",
                table: "school_context_scope_capabilities",
                columns: new[] { "matrix_id", "capability_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_matrices_code",
                table: "school_context_scope_matrices",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_matrices_school_type_code",
                table: "school_context_scope_matrices",
                column: "school_type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_operators_company_number_ico",
                table: "school_operators",
                column: "company_number_ico");

            migrationBuilder.CreateIndex(
                name: "IX_school_operators_legal_entity_name",
                table: "school_operators",
                column: "legal_entity_name");

            migrationBuilder.CreateIndex(
                name: "IX_school_operators_red_izo",
                table: "school_operators",
                column: "red_izo");

            migrationBuilder.CreateIndex(
                name: "IX_school_places_of_education_school_id",
                table: "school_places_of_education",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_places_of_education_school_id_is_primary",
                table: "school_places_of_education",
                columns: new[] { "school_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "IX_school_scope_overrides_matrix_id",
                table: "school_scope_overrides",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_scope_overrides_school_id",
                table: "school_scope_overrides",
                column: "school_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_school_years_school_id",
                table: "school_years",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_schools_founder_id",
                table: "schools",
                column: "founder_id");

            migrationBuilder.CreateIndex(
                name: "IX_schools_is_active",
                table: "schools",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_schools_name",
                table: "schools",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_schools_school_izo",
                table: "schools",
                column: "school_izo");

            migrationBuilder.CreateIndex(
                name: "IX_schools_school_operator_id",
                table: "schools",
                column: "school_operator_id");

            migrationBuilder.CreateIndex(
                name: "IX_schools_school_type",
                table: "schools",
                column: "school_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_rooms");

            migrationBuilder.DropTable(
                name: "grade_levels");

            migrationBuilder.DropTable(
                name: "organization_academic_structure_matrix_entries");

            migrationBuilder.DropTable(
                name: "organization_assignment_matrix_entries");

            migrationBuilder.DropTable(
                name: "organization_capacity_matrix_entries");

            migrationBuilder.DropTable(
                name: "organization_registry_matrix_entries");

            migrationBuilder.DropTable(
                name: "organization_school_structure_matrix_entries");

            migrationBuilder.DropTable(
                name: "role_definitions");

            migrationBuilder.DropTable(
                name: "secondary_fields_of_study");

            migrationBuilder.DropTable(
                name: "school_capacities");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_academics_sections");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_create_user_flows");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_organization_sections");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_profile_sections");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_roles");

            migrationBuilder.DropTable(
                name: "school_context_scope_allowed_user_management_flows");

            migrationBuilder.DropTable(
                name: "school_context_scope_capabilities");

            migrationBuilder.DropTable(
                name: "school_places_of_education");

            migrationBuilder.DropTable(
                name: "school_scope_overrides");

            migrationBuilder.DropTable(
                name: "school_years");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "teacher_assignments");

            migrationBuilder.DropTable(
                name: "teaching_groups");

            migrationBuilder.DropTable(
                name: "school_context_scope_matrices");

            migrationBuilder.DropTable(
                name: "schools");

            migrationBuilder.DropTable(
                name: "founders");

            migrationBuilder.DropTable(
                name: "school_operators");
        }
    }
}
