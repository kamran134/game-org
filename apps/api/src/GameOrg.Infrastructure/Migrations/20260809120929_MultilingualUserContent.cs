using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultilingualUserContent : Migration
    {
        /// <inheritdoc />
        // EF-скаффолд сначала дропал старые скалярные колонки и только потом
        // добавлял *_i18n — данные терялись бы безвозвратно. Переписано руками
        // по плану (docs/PLAN.md, Шаг 7.5): добавить nullable → бэкфилл под
        // ключ "az" (дефолт сайта) → NOT NULL там, где домен требует required →
        // дропнуть старое → search_text (GENERATED ALWAYS AS ... STORED) вместо
        // venues_search_trgm/users_search_trgm, которые висели на удаляемых колонках.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_i18n",
                table: "venues",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_i18n",
                table: "venues",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_i18n",
                table: "venues",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name_i18n",
                table: "users",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio_i18n",
                table: "users",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE venues SET name_i18n = jsonb_build_object('az', name);
                UPDATE venues SET description_i18n = jsonb_build_object('az', description) WHERE description IS NOT NULL;
                UPDATE venues SET address_i18n = jsonb_build_object('az', address) WHERE address IS NOT NULL;
                UPDATE users SET display_name_i18n = jsonb_build_object('az', display_name);
                UPDATE users SET bio_i18n = jsonb_build_object('az', bio) WHERE bio IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "name_i18n",
                table: "venues",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "display_name_i18n",
                table: "users",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "name", table: "venues");
            migrationBuilder.DropColumn(name: "description", table: "venues");
            migrationBuilder.DropColumn(name: "address", table: "venues");
            migrationBuilder.DropColumn(name: "display_name", table: "users");
            migrationBuilder.DropColumn(name: "bio", table: "users");

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS venues_search_trgm;
                ALTER TABLE venues ADD COLUMN search_text text GENERATED ALWAYS AS (
                  coalesce(name_i18n->>'az', '')    || ' ' || coalesce(name_i18n->>'ru', '')    || ' ' ||
                  coalesce(name_i18n->>'en', '')    || ' ' || coalesce(address_i18n->>'az', '') || ' ' ||
                  coalesce(address_i18n->>'ru', '') || ' ' || coalesce(address_i18n->>'en', '')
                ) STORED;
                CREATE INDEX venues_search_trgm ON venues USING GIN (search_text gin_trgm_ops);

                DROP INDEX IF EXISTS users_search_trgm;
                ALTER TABLE users ADD COLUMN search_text text GENERATED ALWAYS AS (
                  coalesce(display_name_i18n->>'az', '') || ' ' ||
                  coalesce(display_name_i18n->>'ru', '') || ' ' ||
                  coalesce(display_name_i18n->>'en', '') || ' ' || handle
                ) STORED;
                CREATE INDEX users_search_trgm ON users USING GIN (search_text gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS venues_search_trgm;
                ALTER TABLE venues DROP COLUMN search_text;

                DROP INDEX IF EXISTS users_search_trgm;
                ALTER TABLE users DROP COLUMN search_text;
                """);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "venues",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "venues",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "venues",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Фолбэк az → en → ru — та же логика, что Localized.Resolve в C#.
            migrationBuilder.Sql("""
                UPDATE venues SET name = coalesce(name_i18n->>'az', name_i18n->>'en', name_i18n->>'ru', '');
                UPDATE venues SET description = coalesce(description_i18n->>'az', description_i18n->>'en', description_i18n->>'ru');
                UPDATE venues SET address = coalesce(address_i18n->>'az', address_i18n->>'en', address_i18n->>'ru');
                UPDATE users SET display_name = coalesce(display_name_i18n->>'az', display_name_i18n->>'en', display_name_i18n->>'ru', '');
                UPDATE users SET bio = coalesce(bio_i18n->>'az', bio_i18n->>'en', bio_i18n->>'ru');
                """);

            migrationBuilder.DropColumn(name: "name_i18n", table: "venues");
            migrationBuilder.DropColumn(name: "description_i18n", table: "venues");
            migrationBuilder.DropColumn(name: "address_i18n", table: "venues");
            migrationBuilder.DropColumn(name: "display_name_i18n", table: "users");
            migrationBuilder.DropColumn(name: "bio_i18n", table: "users");

            migrationBuilder.Sql("""
                CREATE INDEX venues_search_trgm ON venues USING GIN (name gin_trgm_ops, address gin_trgm_ops);
                CREATE INDEX users_search_trgm ON users USING GIN (display_name gin_trgm_ops, handle gin_trgm_ops);
                """);
        }
    }
}
