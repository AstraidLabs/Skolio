using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Organization.Infrastructure.Persistence.Migrations
{
    public partial class Phase3SchoolContextMatrix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extend school_operators: RED_IZO, operator email, data box
            migrationBuilder.Sql("ALTER TABLE school_operators ADD COLUMN IF NOT EXISTS red_izo character varying(32) NULL;");
            migrationBuilder.Sql("ALTER TABLE school_operators ADD COLUMN IF NOT EXISTS operator_email character varying(256) NULL;");
            migrationBuilder.Sql("ALTER TABLE school_operators ADD COLUMN IF NOT EXISTS data_box character varying(64) NULL;");

            // Extend founders: data box
            migrationBuilder.Sql("ALTER TABLE founders ADD COLUMN IF NOT EXISTS founder_data_box character varying(64) NULL;");

            // school_context_scope_matrices — one per SchoolType (unique constraint on school_type_code)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_matrices (
                    id uuid NOT NULL,
                    school_type_code character varying(32) NOT NULL,
                    code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    description character varying(500) NULL,
                    CONSTRAINT "PK_school_context_scope_matrices" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_capabilities — capability rows per matrix
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_capabilities (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    capability_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    is_enabled boolean NOT NULL,
                    CONSTRAINT "PK_school_context_scope_capabilities" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_roles
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_roles (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    role_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_roles" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_profile_sections
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_profile_sections (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    section_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_profile_sections" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_create_user_flows
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_create_user_flows (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    flow_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_create_user_flows" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_user_management_flows
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_user_management_flows (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    flow_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_user_management_flows" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_organization_sections
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_organization_sections (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    section_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_organization_sections" PRIMARY KEY (id)
                );
                """);

            // school_context_scope_allowed_academics_sections
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_context_scope_allowed_academics_sections (
                    id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    section_code character varying(64) NOT NULL,
                    translation_key character varying(128) NOT NULL,
                    CONSTRAINT "PK_school_context_scope_allowed_academics_sections" PRIMARY KEY (id)
                );
                """);

            // school_scope_overrides — optional per-school restriction subset
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_scope_overrides (
                    id uuid NOT NULL,
                    school_id uuid NOT NULL,
                    matrix_id uuid NOT NULL,
                    override_uses_classes boolean NULL,
                    override_uses_groups boolean NULL,
                    override_uses_subjects boolean NULL,
                    override_uses_field_of_study boolean NULL,
                    override_uses_daily_reports boolean NULL,
                    override_uses_attendance boolean NULL,
                    override_uses_grades boolean NULL,
                    override_uses_homework boolean NULL,
                    CONSTRAINT "PK_school_scope_overrides" PRIMARY KEY (id)
                );
                """);

            // Unique constraints
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_matrices_school_type_code\" ON school_context_scope_matrices (school_type_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_matrices_code\" ON school_context_scope_matrices (code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_capabilities_matrix_capability\" ON school_context_scope_capabilities (matrix_id, capability_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_roles_matrix_role\" ON school_context_scope_allowed_roles (matrix_id, role_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_profile_sections_matrix_section\" ON school_context_scope_allowed_profile_sections (matrix_id, section_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_create_user_flows_matrix_flow\" ON school_context_scope_allowed_create_user_flows (matrix_id, flow_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_user_management_flows_matrix_flow\" ON school_context_scope_allowed_user_management_flows (matrix_id, flow_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_organization_sections_matrix_section\" ON school_context_scope_allowed_organization_sections (matrix_id, section_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_context_scope_allowed_academics_sections_matrix_section\" ON school_context_scope_allowed_academics_sections (matrix_id, section_code);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_school_scope_overrides_school_id\" ON school_scope_overrides (school_id);");

            // Additional indexes
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_operators_red_izo\" ON school_operators (red_izo);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_capabilities_matrix_id\" ON school_context_scope_capabilities (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_roles_matrix_id\" ON school_context_scope_allowed_roles (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_profile_sections_matrix_id\" ON school_context_scope_allowed_profile_sections (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_create_user_flows_matrix_id\" ON school_context_scope_allowed_create_user_flows (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_user_management_flows_matrix_id\" ON school_context_scope_allowed_user_management_flows (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_organization_sections_matrix_id\" ON school_context_scope_allowed_organization_sections (matrix_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_context_scope_allowed_academics_sections_matrix_id\" ON school_context_scope_allowed_academics_sections (matrix_id);");

            // Foreign keys for all child tables → matrices
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_capabilities_matrices') THEN
                        ALTER TABLE school_context_scope_capabilities
                        ADD CONSTRAINT "FK_scope_capabilities_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_roles_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_roles
                        ADD CONSTRAINT "FK_scope_allowed_roles_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_profile_sections_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_profile_sections
                        ADD CONSTRAINT "FK_scope_allowed_profile_sections_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_create_user_flows_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_create_user_flows
                        ADD CONSTRAINT "FK_scope_allowed_create_user_flows_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_user_management_flows_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_user_management_flows
                        ADD CONSTRAINT "FK_scope_allowed_user_management_flows_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_organization_sections_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_organization_sections
                        ADD CONSTRAINT "FK_scope_allowed_organization_sections_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_scope_allowed_academics_sections_matrices') THEN
                        ALTER TABLE school_context_scope_allowed_academics_sections
                        ADD CONSTRAINT "FK_scope_allowed_academics_sections_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            // Foreign keys for school_scope_overrides → schools and matrices
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_school_scope_overrides_schools') THEN
                        ALTER TABLE school_scope_overrides
                        ADD CONSTRAINT "FK_school_scope_overrides_schools"
                        FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_school_scope_overrides_matrices') THEN
                        ALTER TABLE school_scope_overrides
                        ADD CONSTRAINT "FK_school_scope_overrides_matrices"
                        FOREIGN KEY (matrix_id) REFERENCES school_context_scope_matrices (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop school_scope_overrides first (references schools and matrices)
            migrationBuilder.Sql("ALTER TABLE school_scope_overrides DROP CONSTRAINT IF EXISTS \"FK_school_scope_overrides_matrices\";");
            migrationBuilder.Sql("ALTER TABLE school_scope_overrides DROP CONSTRAINT IF EXISTS \"FK_school_scope_overrides_schools\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_scope_overrides;");

            // Drop allowed child tables
            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_academics_sections DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_academics_sections_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_academics_sections;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_organization_sections DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_organization_sections_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_organization_sections;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_user_management_flows DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_user_management_flows_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_user_management_flows;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_create_user_flows DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_create_user_flows_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_create_user_flows;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_profile_sections DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_profile_sections_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_profile_sections;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_allowed_roles DROP CONSTRAINT IF EXISTS \"FK_scope_allowed_roles_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_allowed_roles;");

            migrationBuilder.Sql("ALTER TABLE school_context_scope_capabilities DROP CONSTRAINT IF EXISTS \"FK_scope_capabilities_matrices\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_capabilities;");

            // Drop parent matrix table
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_context_scope_matrices;");

            // Revert founder and school_operator columns
            migrationBuilder.Sql("ALTER TABLE founders DROP COLUMN IF EXISTS founder_data_box;");
            migrationBuilder.Sql("ALTER TABLE school_operators DROP COLUMN IF EXISTS data_box;");
            migrationBuilder.Sql("ALTER TABLE school_operators DROP COLUMN IF EXISTS operator_email;");
            migrationBuilder.Sql("ALTER TABLE school_operators DROP COLUMN IF EXISTS red_izo;");
        }
    }
}
