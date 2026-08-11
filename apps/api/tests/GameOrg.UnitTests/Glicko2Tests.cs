using GameOrg.Api.Features.Reputation;

namespace GameOrg.UnitTests;

public class Glicko2Tests
{
    // Контрольные числа — рабочий пример из Glickman, "Example of the Glicko-2 System" (2013).
    [Fact]
    public void Calculate_matches_glickman_worked_example()
    {
        var player = new Glicko2.Rating(1500, 200, 0.06);
        var games = new[]
        {
            new Glicko2.GameResult(new Glicko2.Rating(1400, 30, 0.06), 1),
            new Glicko2.GameResult(new Glicko2.Rating(1550, 100, 0.06), 0),
            new Glicko2.GameResult(new Glicko2.Rating(1700, 300, 0.06), 0),
        };

        var result = Glicko2.Calculate(player, games);

        Assert.Equal(1464.06, result.Value, 1);
        Assert.Equal(151.52, result.Deviation, 1);
        Assert.Equal(0.05999, result.Volatility, 4);
    }

    [Fact]
    public void Calculate_without_games_only_grows_deviation()
    {
        var player = new Glicko2.Rating(1500, 200, 0.06);

        var result = Glicko2.Calculate(player, []);

        Assert.Equal(1500, result.Value);
        Assert.Equal(0.06, result.Volatility);
        Assert.True(result.Deviation > 200, "Неопределённость должна вырасти без игр за период.");
    }

    [Fact]
    public void Calculate_win_increases_rating()
    {
        var player = new Glicko2.Rating(1500, 200, 0.06);
        var games = new[] { new Glicko2.GameResult(new Glicko2.Rating(1500, 200, 0.06), 1) };

        var result = Glicko2.Calculate(player, games);

        Assert.True(result.Value > 1500);
    }

    [Fact]
    public void Calculate_loss_decreases_rating()
    {
        var player = new Glicko2.Rating(1500, 200, 0.06);
        var games = new[] { new Glicko2.GameResult(new Glicko2.Rating(1500, 200, 0.06), 0) };

        var result = Glicko2.Calculate(player, games);

        Assert.True(result.Value < 1500);
    }

    [Fact]
    public void Calculate_draw_between_equal_players_keeps_rating_unchanged()
    {
        var player = new Glicko2.Rating(1500, 200, 0.06);
        var games = new[] { new Glicko2.GameResult(new Glicko2.Rating(1500, 200, 0.06), 0.5) };

        var result = Glicko2.Calculate(player, games);

        Assert.Equal(1500, result.Value, 6);
    }
}
