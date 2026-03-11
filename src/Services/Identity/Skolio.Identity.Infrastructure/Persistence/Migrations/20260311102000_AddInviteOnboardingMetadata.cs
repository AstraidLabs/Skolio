using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddInviteOnboardingMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "invite_status", table: "AspNetUsers", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "invite_sent_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "invite_expires_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "invite_confirmed_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "onboarding_completed_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "invite_token_hash", table: "AspNetUsers", type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>(name: "invite_code_hash", table: "AspNetUsers", type: "character varying(128)", maxLength: 128, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "invite_status", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "invite_sent_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "invite_expires_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "invite_confirmed_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "onboarding_completed_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "invite_token_hash", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "invite_code_hash", table: "AspNetUsers");
    }
}
