using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRequiresApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_approval",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requires_approval",
                table: "events");
        }
    }
}
