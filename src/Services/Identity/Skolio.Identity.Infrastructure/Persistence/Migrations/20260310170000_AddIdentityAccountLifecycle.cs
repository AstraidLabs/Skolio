using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddIdentityAccountLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "account_lifecycle_status",
            table: "AspNetUsers",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "activation_requested_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "activated_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "deactivated_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deactivation_reason",
            table: "AspNetUsers",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deactivated_by_user_id",
            table: "AspNetUsers",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "blocked_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "blocked_reason",
            table: "AspNetUsers",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "blocked_by_user_id",
            table: "AspNetUsers",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "last_login_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "last_activity_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "inactivity_warning_sent_at_utc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "account_lifecycle_status", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "activation_requested_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "activated_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "deactivated_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "deactivation_reason", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "deactivated_by_user_id", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "blocked_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "blocked_reason", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "blocked_by_user_id", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "last_login_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "last_activity_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "inactivity_warning_sent_at_utc", table: "AspNetUsers");
    }
}
