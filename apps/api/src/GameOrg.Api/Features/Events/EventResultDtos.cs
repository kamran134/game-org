using GameOrg.Domain;

namespace GameOrg.Api.Features.Events;

public sealed record EventTeamDto(Guid Id, string Name, string? ColorHex, int? Score, int SortOrder, List<Guid> MemberParticipantIds);

public sealed record TeamInput(string Name, string? ColorHex, int SortOrder, List<Guid> ParticipantIds);

public sealed record SetTeamsRequest(List<TeamInput> Teams);

/// <summary>Для не-командных видов: место + опциональные очки одного участника.</summary>
public sealed record StandingEntry(Guid UserId, int Place, int? Score);

public sealed record TeamScoreEntry(Guid TeamId, int Score);

/// <summary>Status — только Attended/NoShow/LateCancel, остальные ParticipationStatus сюда не подходят.</summary>
public sealed record AttendanceEntry(Guid ParticipantId, ParticipationStatus Status);

/// <summary>MvpUserId — явное переопределение; не передан → берётся победитель голосования на момент записи.</summary>
public sealed record RecordResultRequest(
    string? Summary,
    List<StandingEntry>? Standings,
    List<TeamScoreEntry>? TeamScores,
    List<AttendanceEntry>? Attendance,
    Guid? MvpUserId);

public sealed record EventResultDto(
    string? Summary,
    List<Dictionary<string, object>>? Standings,
    Guid? MvpUserId,
    string? MvpDisplayName,
    Guid? RecordedById,
    DateTime RecordedAt);

public sealed record MvpVoteRequest(Guid TargetUserId);

public sealed record MvpTallyEntryDto(Guid UserId, string DisplayName, int Votes);
