using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skolio.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseValidationOrganizationSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_capabilities_matrix_id",
                table: "school_context_scope_capabilities");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_user_management_flows_matrix_id",
                table: "school_context_scope_allowed_user_management_flows");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_roles_matrix_id",
                table: "school_context_scope_allowed_roles");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_profile_sections_matrix_id",
                table: "school_context_scope_allowed_profile_sections");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_organization_sections_matrix_id",
                table: "school_context_scope_allowed_organization_sections");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_create_user_flows_matrix_id",
                table: "school_context_scope_allowed_create_user_flows");

            migrationBuilder.DropIndex(
                name: "IX_school_context_scope_allowed_academics_sections_matrix_id",
                table: "school_context_scope_allowed_academics_sections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_capabilities_matrix_id",
                table: "school_context_scope_capabilities",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_user_management_flows_matrix_id",
                table: "school_context_scope_allowed_user_management_flows",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_roles_matrix_id",
                table: "school_context_scope_allowed_roles",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_profile_sections_matrix_id",
                table: "school_context_scope_allowed_profile_sections",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_organization_sections_matrix_id",
                table: "school_context_scope_allowed_organization_sections",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_create_user_flows_matrix_id",
                table: "school_context_scope_allowed_create_user_flows",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_context_scope_allowed_academics_sections_matrix_id",
                table: "school_context_scope_allowed_academics_sections",
                column: "matrix_id");
        }
    }
}
