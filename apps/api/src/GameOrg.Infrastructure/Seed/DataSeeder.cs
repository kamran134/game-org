using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Infrastructure.Seed;

/// <summary>
/// Идемпотентные справочники: виды спорта с позициями, города Азербайджана, ачивки.
/// Безопасно запускать повторно — каждый блок проверяет наличие данных перед вставкой.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(GameOrgDbContext db, CancellationToken ct = default)
    {
        await SeedSportsAsync(db, ct);
        await SeedCitiesAsync(db, ct);
        await SeedAchievementsAsync(db, ct);
    }

    private static async Task SeedSportsAsync(GameOrgDbContext db, CancellationToken ct)
    {
        if (await db.Sports.AnyAsync(ct)) return;

        Sport Make(string slug, string emoji, int? teamSize, bool positions, string ru, string az, string en) => new()
        {
            Slug = slug,
            Emoji = emoji,
            DefaultTeamSize = teamSize,
            HasPositions = positions,
            NameI18n = new Dictionary<string, string> { ["ru"] = ru, ["az"] = az, ["en"] = en },
        };

        var football = Make("football", "⚽", 11, true, "Футбол", "Futbol", "Football");
        var futsal = Make("futsal", "⚽", 5, true, "Мини-футбол", "Mini-futbol", "Futsal");
        var volleyball = Make("volleyball", "🏐", 6, true, "Волейбол", "Voleybol", "Volleyball");
        var basketball = Make("basketball", "🏀", 5, true, "Баскетбол", "Basketbol", "Basketball");
        var tennis = Make("tennis", "🎾", null, false, "Теннис", "Tennis", "Tennis");
        var tableTennis = Make("table-tennis", "🏓", null, false, "Настольный теннис", "Stolüstü tennis", "Table tennis");
        var badminton = Make("badminton", "🏸", null, false, "Бадминтон", "Badminton", "Badminton");

        var sports = new[] { football, futsal, volleyball, basketball, tennis, tableTennis, badminton };
        for (var i = 0; i < sports.Length; i++) sports[i].SortOrder = i;
        db.Sports.AddRange(sports);

        SportPosition Pos(Sport sport, string code, string ru, string az, string en, int order) => new()
        {
            Sport = sport,
            Code = code,
            SortOrder = order,
            NameI18n = new Dictionary<string, string> { ["ru"] = ru, ["az"] = az, ["en"] = en },
        };

        db.SportPositions.AddRange(
            Pos(football, "GK", "Вратарь", "Qapıçı", "Goalkeeper", 0),
            Pos(football, "CB", "Центральный защитник", "Mərkəz müdafiəçi", "Centre-back", 1),
            Pos(football, "LB", "Левый защитник", "Sol müdafiəçi", "Left-back", 2),
            Pos(football, "RB", "Правый защитник", "Sağ müdafiəçi", "Right-back", 3),
            Pos(football, "CDM", "Опорный полузащитник", "Mərkəz yarımmüdafiəçi", "Defensive midfielder", 4),
            Pos(football, "CM", "Центральный полузащитник", "Mərkəz yarımmüdafiəçi", "Central midfielder", 5),
            Pos(football, "CAM", "Атакующий полузащитник", "Hücumçu yarımmüdafiəçi", "Attacking midfielder", 6),
            Pos(football, "LW", "Левый вингер", "Sol qanadçı", "Left winger", 7),
            Pos(football, "RW", "Правый вингер", "Sağ qanadçı", "Right winger", 8),
            Pos(football, "ST", "Нападающий", "Hücumçu", "Striker", 9),

            Pos(volleyball, "SETTER", "Связующий", "Pasçı", "Setter", 0),
            Pos(volleyball, "OUTSIDE", "Диагональный", "Kənar hücumçu", "Outside hitter", 1),
            Pos(volleyball, "MIDDLE", "Центральный блокирующий", "Mərkəz bloklayıcı", "Middle blocker", 2),
            Pos(volleyball, "OPPOSITE", "Диагональный нападающий", "Diaqonal", "Opposite", 3),
            Pos(volleyball, "LIBERO", "Либеро", "Libero", "Libero", 4),

            Pos(basketball, "PG", "Разыгрывающий защитник", "Point guard", "Point guard", 0),
            Pos(basketball, "SG", "Атакующий защитник", "Shooting guard", "Shooting guard", 1),
            Pos(basketball, "SF", "Лёгкий форвард", "Small forward", "Small forward", 2),
            Pos(basketball, "PF", "Мощный форвард", "Power forward", "Power forward", 3),
            Pos(basketball, "C", "Центровой", "Center", "Center", 4));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCitiesAsync(GameOrgDbContext db, CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct)) return;

        City Make(string slug, double lat, double lng, string ru, string az, string en, int order) => new()
        {
            Slug = slug,
            Lat = lat,
            Lng = lng,
            CountryCode = "AZ",
            SortOrder = order,
            NameI18n = new Dictionary<string, string> { ["ru"] = ru, ["az"] = az, ["en"] = en },
        };

        db.Cities.AddRange(
            Make("baku", 40.4093, 49.8671, "Баку", "Bakı", "Baku", 0),
            Make("ganja", 40.6828, 46.3606, "Гянджа", "Gəncə", "Ganja", 1),
            Make("sumgayit", 40.5892, 49.6685, "Сумгайыт", "Sumqayıt", "Sumgayit", 2),
            Make("mingachevir", 40.7700, 47.0500, "Мингечевир", "Mingəçevir", "Mingachevir", 3),
            Make("shirvan", 39.9412, 48.9228, "Ширван", "Şirvan", "Shirvan", 4),
            Make("nakhchivan", 39.2089, 45.4122, "Нахчыван", "Naxçıvan", "Nakhchivan", 5),
            Make("sheki", 41.1919, 47.1706, "Шеки", "Şəki", "Sheki", 6),
            Make("yevlakh", 40.6167, 47.1500, "Евлах", "Yevlax", "Yevlakh", 7),
            Make("lankaran", 38.7539, 48.8406, "Ленкорань", "Lənkəran", "Lankaran", 8));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAchievementsAsync(GameOrgDbContext db, CancellationToken ct)
    {
        if (await db.Achievements.AnyAsync(ct)) return;

        Achievement Make(string code, int tier, string icon, string nameRu, string nameAz, string nameEn, string descRu, string descAz, string descEn) => new()
        {
            Code = code,
            Tier = tier,
            Icon = icon,
            NameI18n = new Dictionary<string, string> { ["ru"] = nameRu, ["az"] = nameAz, ["en"] = nameEn },
            DescI18n = new Dictionary<string, string> { ["ru"] = descRu, ["az"] = descAz, ["en"] = descEn },
        };

        db.Achievements.AddRange(
            Make("FIRST_GAME", 1, "🎉", "Первая игра", "İlk oyun", "First game",
                "Сыграл(-а) первую игру на портале", "Portalda ilk oyununu keçirdin", "Played your first game on the platform"),
            Make("TEN_GAMES", 1, "🔟", "10 игр", "10 oyun", "10 games",
                "Сыграл(-а) 10 игр", "10 oyun keçirdin", "Played 10 games"),
            Make("FIFTY_GAMES", 2, "🏅", "50 игр", "50 oyun", "50 games",
                "Сыграл(-а) 50 игр", "50 oyun keçirdin", "Played 50 games"),
            Make("IRON_MAN", 2, "🔥", "Железный человек", "Dəmir adam", "Iron man",
                "4 недели подряд с игрой", "Ardıcıl 4 həftə oyun", "4 consecutive weeks with a game"),
            Make("MVP_FIRST", 1, "⭐", "Первый MVP", "İlk MVP", "First MVP",
                "Признан(-а) MVP матча впервые", "İlk dəfə matçın MVP-si oldun", "Voted MVP of a match for the first time"),
            Make("RELIABLE", 2, "✅", "Надёжный игрок", "Etibarlı oyunçu", "Reliable player",
                "20 игр без пропусков", "Buraxılış olmadan 20 oyun", "20 games without a no-show"),
            Make("MULTI_SPORT", 1, "🎽", "Разносторонний", "Çoxşaxəli", "Multi-sport",
                "Играет в 2+ вида спорта", "2+ növ idmanla məşğul olur", "Plays 2+ sports"));

        await db.SaveChangesAsync(ct);
    }
}
