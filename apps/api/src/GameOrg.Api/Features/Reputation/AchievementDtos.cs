namespace GameOrg.Api.Features.Reputation;

/// <summary>EarnedAt — null в каталоге (GetMyAchievementsAsync) для ещё не полученных, всегда заполнен в профиле.</summary>
public sealed record AchievementDto(string Code, string Name, string? Description, string? Icon, int Tier, DateTime? EarnedAt);
