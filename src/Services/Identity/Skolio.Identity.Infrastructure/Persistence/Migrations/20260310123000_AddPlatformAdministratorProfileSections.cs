using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddPlatformAdministratorProfileSections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "platform_role_context_summary",
            table: "user_profiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "managed_platform_areas_summary",
            table: "user_profiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "administrative_boundary_summary",
            table: "user_profiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "platform_role_context_summary",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "managed_platform_areas_summary",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "administrative_boundary_summary",
            table: "user_profiles");
    }
}
