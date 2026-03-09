using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddStudentProfileSections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "gender", table: "user_profiles", type: "character varying(16)", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "date_of_birth", table: "user_profiles", type: "date", nullable: true);
        migrationBuilder.AddColumn<string>(name: "national_id_number", table: "user_profiles", type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "birth_place", table: "user_profiles", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>(name: "permanent_address", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "correspondence_address", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "contact_email", table: "user_profiles", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "legal_guardian_1", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "legal_guardian_2", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "school_placement", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "health_insurance_provider", table: "user_profiles", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>(name: "pediatrician", table: "user_profiles", type: "character varying(240)", maxLength: 240, nullable: true);
        migrationBuilder.AddColumn<string>(name: "health_safety_notes", table: "user_profiles", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "support_measures_summary", table: "user_profiles", type: "character varying(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "gender", table: "user_profiles");
        migrationBuilder.DropColumn(name: "date_of_birth", table: "user_profiles");
        migrationBuilder.DropColumn(name: "national_id_number", table: "user_profiles");
        migrationBuilder.DropColumn(name: "birth_place", table: "user_profiles");
        migrationBuilder.DropColumn(name: "permanent_address", table: "user_profiles");
        migrationBuilder.DropColumn(name: "correspondence_address", table: "user_profiles");
        migrationBuilder.DropColumn(name: "contact_email", table: "user_profiles");
        migrationBuilder.DropColumn(name: "legal_guardian_1", table: "user_profiles");
        migrationBuilder.DropColumn(name: "legal_guardian_2", table: "user_profiles");
        migrationBuilder.DropColumn(name: "school_placement", table: "user_profiles");
        migrationBuilder.DropColumn(name: "health_insurance_provider", table: "user_profiles");
        migrationBuilder.DropColumn(name: "pediatrician", table: "user_profiles");
        migrationBuilder.DropColumn(name: "health_safety_notes", table: "user_profiles");
        migrationBuilder.DropColumn(name: "support_measures_summary", table: "user_profiles");
    }
}
