using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueClaimResolvedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_id",
                table: "venue_claims",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_claims_resolved_by_id",
                table: "venue_claims",
                column: "resolved_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_venue_claims_users_resolved_by_id",
                table: "venue_claims",
                column: "resolved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_venue_claims_users_resolved_by_id",
                table: "venue_claims");

            migrationBuilder.DropIndex(
                name: "ix_venue_claims_resolved_by_id",
                table: "venue_claims");

            migrationBuilder.DropColumn(
                name: "resolved_by_id",
                table: "venue_claims");
        }
    }
}
