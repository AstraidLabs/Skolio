using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddTeacherProfileSections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "teacher_role_label",
            table: "user_profiles",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "qualification_summary",
            table: "user_profiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "school_context_summary",
            table: "user_profiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "teacher_role_label",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "qualification_summary",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "school_context_summary",
            table: "user_profiles");
    }
}
