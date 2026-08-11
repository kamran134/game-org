namespace GameOrg.Api.Features.Reputation;

/// <summary>
/// Glicko-2 (Glickman, "Example of the Glicko-2 System", 2013) — чистая математика, без
/// побочных эффектов и без БД (как TelegramLoginValidator в Шаге 5 — юнит-тестируется напрямую).
/// Один вызов Calculate = один rating period для игрока: все его игры за одно событие
/// сворачиваются в одно обновление рейтинга/отклонения/волатильности.
/// </summary>
public static class Glicko2
{
    private const double ScaleFactor = 173.7178;
    private const double Tau = 0.5;
    private const double Epsilon = 0.000001;

    public readonly record struct Rating(double Value, double Deviation, double Volatility);

    public readonly record struct GameResult(Rating Opponent, double Score);

    /// <summary>Score — 1 (победа), 0.5 (ничья) или 0 (поражение) с точки зрения player.</summary>
    public static Rating Calculate(Rating player, IReadOnlyList<GameResult> games)
    {
        var mu = (player.Value - 1500) / ScaleFactor;
        var phi = player.Deviation / ScaleFactor;
        var sigma = player.Volatility;

        // Не сыграл за период — растёт только неопределённость (φ), рейтинг и волатильность не трогаем.
        if (games.Count == 0)
        {
            var phiUnplayed = Math.Sqrt(phi * phi + sigma * sigma);
            return new Rating(player.Value, phiUnplayed * ScaleFactor, sigma);
        }

        var opponents = games
            .Select(g => (Mu: (g.Opponent.Value - 1500) / ScaleFactor, Phi: g.Opponent.Deviation / ScaleFactor, g.Score))
            .ToList();

        double G(double oppPhi) => 1 / Math.Sqrt(1 + 3 * oppPhi * oppPhi / (Math.PI * Math.PI));
        double E(double oppMu, double oppPhi) => 1 / (1 + Math.Exp(-G(oppPhi) * (mu - oppMu)));

        var vInverse = 0.0;
        var deltaSum = 0.0;
        foreach (var (oppMu, oppPhi, score) in opponents)
        {
            var g = G(oppPhi);
            var e = E(oppMu, oppPhi);
            vInverse += g * g * e * (1 - e);
            deltaSum += g * (score - e);
        }

        var v = 1 / vInverse;
        var delta = v * deltaSum;

        var newSigma = SolveNewVolatility(delta, phi, v, sigma);

        var phiStar = Math.Sqrt(phi * phi + newSigma * newSigma);
        var newPhi = 1 / Math.Sqrt(1 / (phiStar * phiStar) + 1 / v);
        var newMu = mu + newPhi * newPhi * deltaSum;

        return new Rating(ScaleFactor * newMu + 1500, ScaleFactor * newPhi, newSigma);
    }

    /// <summary>Итерационный метод Illinois — решает f(x) = 0 относительно новой волатильности.</summary>
    private static double SolveNewVolatility(double delta, double phi, double v, double sigma)
    {
        var a = Math.Log(sigma * sigma);

        double F(double x)
        {
            var ex = Math.Exp(x);
            var num = ex * (delta * delta - phi * phi - v - ex);
            var den = 2 * Math.Pow(phi * phi + v + ex, 2);
            return num / den - (x - a) / (Tau * Tau);
        }

        var A = a;
        double B;
        if (delta * delta > phi * phi + v)
        {
            B = Math.Log(delta * delta - phi * phi - v);
        }
        else
        {
            var k = 1;
            while (F(a - k * Tau) < 0) k++;
            B = a - k * Tau;
        }

        var fA = F(A);
        var fB = F(B);
        while (Math.Abs(B - A) > Epsilon)
        {
            var C = A + (A - B) * fA / (fB - fA);
            var fC = F(C);
            if (fC * fB <= 0)
            {
                A = B;
                fA = fB;
            }
            else
            {
                fA /= 2;
            }

            B = C;
            fB = fC;
        }

        return Math.Exp(A / 2);
    }
}
