using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddSchoolAdministratorProfileSections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "administrative_work_designation",
            table: "user_profiles",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "administrative_organization_summary",
            table: "user_profiles",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "administrative_work_designation",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "administrative_organization_summary",
            table: "user_profiles");
    }
}
