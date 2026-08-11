using GameOrg.Api.Features.Reputation;

namespace GameOrg.UnitTests;

public class ReliabilityCalculatorTests
{
    [Fact]
    public void ComputeScore_no_history_returns_100()
    {
        Assert.Equal(100, ReliabilityCalculator.ComputeScore(0, 0, 0));
    }

    [Fact]
    public void ComputeScore_only_no_shows_is_low()
    {
        var score = ReliabilityCalculator.ComputeScore(attended: 0, noShows: 3, lateCancels: 0);
        Assert.Equal(0, score);
    }

    [Fact]
    public void ComputeScore_late_cancel_penalized_less_than_no_show()
    {
        var lateCancelScore = ReliabilityCalculator.ComputeScore(attended: 9, noShows: 0, lateCancels: 1);
        var noShowScore = ReliabilityCalculator.ComputeScore(attended: 9, noShows: 1, lateCancels: 0);
        Assert.True(lateCancelScore > noShowScore);
    }

    [Fact]
    public void ComputeScore_old_violation_diluted_by_many_attendances()
    {
        var early = ReliabilityCalculator.ComputeScore(attended: 1, noShows: 1, lateCancels: 0);
        var later = ReliabilityCalculator.ComputeScore(attended: 20, noShows: 1, lateCancels: 0);
        Assert.True(later > early);
    }

    [Fact]
    public void ComputeStreaks_no_history_returns_zero()
    {
        var (current, longest) = ReliabilityCalculator.ComputeStreaks([], DateTime.UtcNow);
        Assert.Equal(0, current);
        Assert.Equal(0, longest);
    }

    [Fact]
    public void ComputeStreaks_consecutive_weeks_count_correctly()
    {
        var now = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc); // вторник
        var dates = new[]
        {
            now,
            now.AddDays(-7),
            now.AddDays(-14),
        };

        var (current, longest) = ReliabilityCalculator.ComputeStreaks(dates, now);

        Assert.Equal(3, current);
        Assert.Equal(3, longest);
    }

    [Fact]
    public void ComputeStreaks_multiple_games_in_same_week_count_once()
    {
        var now = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
        var dates = new[] { now, now.AddDays(-1), now.AddDays(1) };

        var (current, longest) = ReliabilityCalculator.ComputeStreaks(dates, now);

        Assert.Equal(1, current);
        Assert.Equal(1, longest);
    }

    [Fact]
    public void ComputeStreaks_gap_breaks_current_streak_but_keeps_longest()
    {
        var now = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
        var dates = new[]
        {
            now.AddDays(-42), // старая серия из трёх недель подряд
            now.AddDays(-49),
            now.AddDays(-56),
        };

        var (current, longest) = ReliabilityCalculator.ComputeStreaks(dates, now);

        Assert.Equal(0, current);
        Assert.Equal(3, longest);
    }

    [Fact]
    public void ComputeStreaks_last_week_without_current_week_game_still_alive()
    {
        var now = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
        var dates = new[] { now.AddDays(-7), now.AddDays(-14) };

        var (current, _) = ReliabilityCalculator.ComputeStreaks(dates, now);

        Assert.Equal(2, current);
    }
}
