using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddParentProfileSections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "parent_relationship_summary",
            table: "user_profiles",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "delivery_contact_name",
            table: "user_profiles",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "delivery_contact_phone",
            table: "user_profiles",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "preferred_contact_channel",
            table: "user_profiles",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "communication_preferences_summary",
            table: "user_profiles",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "parent_relationship_summary",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "delivery_contact_name",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "delivery_contact_phone",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "preferred_contact_channel",
            table: "user_profiles");

        migrationBuilder.DropColumn(
            name: "communication_preferences_summary",
            table: "user_profiles");
    }
}
