using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Identity.Infrastructure.Persistence.Migrations;

public partial class AddBootstrapPlatformAdminMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "is_bootstrap_platform_administrator", table: "AspNetUsers", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "bootstrap_mfa_completed_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "bootstrap_activation_completed_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "bootstrap_first_login_completed_at_utc", table: "AspNetUsers", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "is_bootstrap_platform_administrator", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "bootstrap_mfa_completed_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "bootstrap_activation_completed_at_utc", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "bootstrap_first_login_completed_at_utc", table: "AspNetUsers");
    }
}
