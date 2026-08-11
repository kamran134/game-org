using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClubMultilingualContent : Migration
    {
        /// <inheritdoc />
        // Тот же ручной приём, что MultilingualUserContent (Шаг 7.5) — EF-скаффолд
        // дропал бы name/description раньше бэкфилла, данные терялись бы. Клубов
        // ещё нет ни одного (Шаг 12 — первый код для Club), но пишем миграцию так,
        // будто данные есть, — тот же путь для dev/prod, не полагаться на пустоту таблицы.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_i18n",
                table: "clubs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_i18n",
                table: "clubs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE clubs SET name_i18n = jsonb_build_object('az', name);
                UPDATE clubs SET description_i18n = jsonb_build_object('az', description) WHERE description IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "name_i18n",
                table: "clubs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "name", table: "clubs");
            migrationBuilder.DropColumn(name: "description", table: "clubs");

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS clubs_search_trgm;
                ALTER TABLE clubs ADD COLUMN search_text text GENERATED ALWAYS AS (
                  coalesce(name_i18n->>'az', '') || ' ' ||
                  coalesce(name_i18n->>'ru', '') || ' ' ||
                  coalesce(name_i18n->>'en', '')
                ) STORED;
                CREATE INDEX clubs_search_trgm ON clubs USING GIN (search_text gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS clubs_search_trgm;
                ALTER TABLE clubs DROP COLUMN search_text;
                """);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "clubs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "clubs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Фолбэк az → en → ru — та же логика, что Localized.Resolve в C#.
            migrationBuilder.Sql("""
                UPDATE clubs SET name = coalesce(name_i18n->>'az', name_i18n->>'en', name_i18n->>'ru', '');
                UPDATE clubs SET description = coalesce(description_i18n->>'az', description_i18n->>'en', description_i18n->>'ru');
                """);

            migrationBuilder.DropColumn(name: "name_i18n", table: "clubs");
            migrationBuilder.DropColumn(name: "description_i18n", table: "clubs");

            migrationBuilder.Sql("""
                CREATE INDEX clubs_search_trgm ON clubs USING GIN (name gin_trgm_ops);
                """);
        }
    }
}
