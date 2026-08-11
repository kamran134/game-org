namespace GameOrg.Api.Features.Reputation;

/// <summary>
/// Чистые функции без БД (юнит-тестируются напрямую). ReliabilityStat.Score в схеме
/// описан как "с затуханием старых нарушений", но история инцидентов с датами в схеме
/// не хранится (только кумулятивные счётчики) — заводить отдельную таблицу ради честного
/// затухания не входит в объём Шага 15. Вместо этого — доля: старое единичное нарушение
/// теряет вес по мере роста знаменателя (игрок "отыгрывает" репутацию явками), тот же
/// практический эффект без истории.
/// </summary>
public static class ReliabilityCalculator
{
    public static int ComputeScore(int attended, int noShows, int lateCancels)
    {
        var total = attended + noShows + lateCancels;
        if (total == 0) return 100;

        var unreliableWeight = noShows + lateCancels * 0.5;
        var score = 100 * (1 - unreliableWeight / total);
        return (int)Math.Round(Math.Clamp(score, 0, 100));
    }

    /// <summary>
    /// "Недель подряд с игрой" — считается с нуля по факту явок, не инкрементом. Несколько
    /// игр в одну неделю считаются одной. Текущая календарная неделя без игры пока не рвёт
    /// CurrentStreak — она ещё не закончилась (последняя сыгранная неделя не старше предыдущей).
    /// </summary>
    public static (int Current, int Longest) ComputeStreaks(IReadOnlyList<DateTime> attendedDates, DateTime now)
    {
        if (attendedDates.Count == 0) return (0, 0);

        var weekStarts = attendedDates.Select(StartOfWeek).Distinct().OrderBy(d => d).ToList();

        var longest = 1;
        var run = 1;
        for (var i = 1; i < weekStarts.Count; i++)
        {
            run = (weekStarts[i] - weekStarts[i - 1]).Days == 7 ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        var weeksSinceLast = (StartOfWeek(now) - weekStarts[^1]).Days / 7;
        if (weeksSinceLast > 1) return (0, longest);

        var current = 1;
        for (var i = weekStarts.Count - 1; i > 0; i--)
        {
            if ((weekStarts[i] - weekStarts[i - 1]).Days != 7) break;
            current++;
        }

        return (current, longest);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        var diff = day == 0 ? 6 : day - 1; // Monday = начало недели
        return date.Date.AddDays(-diff);
    }
}
