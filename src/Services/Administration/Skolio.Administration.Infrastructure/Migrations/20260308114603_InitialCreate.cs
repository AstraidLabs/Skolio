using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Administration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_toggles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_toggles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "housekeeping_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_housekeeping_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_year_lifecycle_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    closure_grace_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_year_lifecycle_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_action_code",
                table: "audit_log_entries",
                column: "action_code");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_actor_user_id",
                table: "audit_log_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_created_at_utc",
                table: "audit_log_entries",
                column: "created_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log_entries");

            migrationBuilder.DropTable(
                name: "feature_toggles");

            migrationBuilder.DropTable(
                name: "housekeeping_policies");

            migrationBuilder.DropTable(
                name: "school_year_lifecycle_policies");

            migrationBuilder.DropTable(
                name: "system_settings");
        }
    }
}
