using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventTitleDescriptionI18n : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description_i18n",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_i18n",
                table: "events",
                type: "jsonb",
                nullable: true);

            // Бэкфилл в az (дефолт сайта) — до удаления старых скалярных
            // колонок ниже. NULL остаётся NULL (не оборачиваем в
            // jsonb_build_object, если исходного текста не было).
            migrationBuilder.Sql(
                "UPDATE events SET title_i18n = jsonb_build_object('az', title) WHERE title IS NOT NULL;");
            migrationBuilder.Sql(
                "UPDATE events SET description_i18n = jsonb_build_object('az', description) WHERE description IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "description",
                table: "events");

            migrationBuilder.DropColumn(
                name: "title",
                table: "events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_i18n",
                table: "events");

            migrationBuilder.DropColumn(
                name: "title_i18n",
                table: "events");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "events",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "events",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }
    }
}
