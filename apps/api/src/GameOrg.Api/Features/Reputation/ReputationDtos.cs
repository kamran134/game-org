namespace GameOrg.Api.Features.Reputation;

public sealed record LeaderboardEntryDto(
    Guid UserId,
    string Handle,
    string DisplayName,
    double Rating,
    int GamesPlayed,
    int Wins,
    int Draws,
    int Losses);
