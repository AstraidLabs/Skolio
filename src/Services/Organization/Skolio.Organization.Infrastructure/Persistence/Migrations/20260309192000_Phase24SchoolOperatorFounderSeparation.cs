using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Organization.Infrastructure.Persistence.Migrations
{
    public partial class Phase24SchoolOperatorFounderSeparation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS school_operators (
                    id uuid NOT NULL,
                    legal_entity_name character varying(200) NOT NULL,
                    legal_form character varying(64) NOT NULL,
                    company_number_ico character varying(32) NULL,
                    registered_office_street character varying(200) NOT NULL,
                    registered_office_city character varying(120) NOT NULL,
                    registered_office_postal_code character varying(32) NOT NULL,
                    registered_office_country character varying(120) NOT NULL,
                    resort_identifier character varying(64) NULL,
                    director_summary character varying(300) NULL,
                    statutory_body_summary character varying(600) NULL,
                    CONSTRAINT "PK_school_operators" PRIMARY KEY (id)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS founders (
                    id uuid NOT NULL,
                    founder_type character varying(64) NOT NULL,
                    founder_category character varying(64) NOT NULL,
                    founder_name character varying(200) NOT NULL,
                    founder_legal_form character varying(64) NOT NULL,
                    founder_ico character varying(32) NULL,
                    founder_address_street character varying(200) NOT NULL,
                    founder_address_city character varying(120) NOT NULL,
                    founder_address_postal_code character varying(32) NOT NULL,
                    founder_address_country character varying(120) NOT NULL,
                    founder_email character varying(256) NULL,
                    CONSTRAINT "PK_founders" PRIMARY KEY (id)
                );
                """);

            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_kind character varying(32) NOT NULL DEFAULT 'General';");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_izo character varying(32) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_email character varying(256) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_phone character varying(64) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_website character varying(256) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS main_address_street character varying(200) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS main_address_city character varying(120) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS main_address_postal_code character varying(32) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS main_address_country character varying(120) NOT NULL DEFAULT 'CZ';");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS education_locations_summary character varying(1000) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS registry_entry_date date NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS education_start_date date NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS max_student_capacity integer NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS teaching_language character varying(64) NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS school_operator_id uuid NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS founder_id uuid NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ADD COLUMN IF NOT EXISTS platform_status character varying(32) NOT NULL DEFAULT 'Active';");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_operators_legal_entity_name\" ON school_operators (legal_entity_name);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_school_operators_company_number_ico\" ON school_operators (company_number_ico);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_founders_founder_type\" ON founders (founder_type);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_founders_founder_category\" ON founders (founder_category);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_schools_school_izo\" ON schools (school_izo);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_schools_school_operator_id\" ON schools (school_operator_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_schools_founder_id\" ON schools (founder_id);");

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_schools_school_operators_school_operator_id'
                    ) THEN
                        ALTER TABLE schools
                        ADD CONSTRAINT "FK_schools_school_operators_school_operator_id"
                        FOREIGN KEY (school_operator_id)
                        REFERENCES school_operators (id)
                        ON DELETE RESTRICT;
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_schools_founders_founder_id'
                    ) THEN
                        ALTER TABLE schools
                        ADD CONSTRAINT "FK_schools_founders_founder_id"
                        FOREIGN KEY (founder_id)
                        REFERENCES founders (id)
                        ON DELETE RESTRICT;
                    END IF;
                END
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE schools DROP CONSTRAINT IF EXISTS \"FK_schools_school_operators_school_operator_id\";");
            migrationBuilder.Sql("ALTER TABLE schools DROP CONSTRAINT IF EXISTS \"FK_schools_founders_founder_id\";");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_schools_founder_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_schools_school_operator_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_schools_school_izo\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_founders_founder_category\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_founders_founder_type\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_school_operators_company_number_ico\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_school_operators_legal_entity_name\";");

            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS platform_status;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS founder_id;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_operator_id;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS teaching_language;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS max_student_capacity;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS education_start_date;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS registry_entry_date;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS education_locations_summary;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS main_address_country;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS main_address_postal_code;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS main_address_city;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS main_address_street;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_website;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_phone;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_email;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_izo;");
            migrationBuilder.Sql("ALTER TABLE schools DROP COLUMN IF EXISTS school_kind;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS founders;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS school_operators;");
        }
    }
}
