using GameOrg.Api.Common;
using GameOrg.Api.Features.Payments;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Events;

public sealed record EventVenueDto(Guid Id, string Slug, string Name);

public sealed record EventDto(
    Guid Id,
    string PublicId,
    EventType Type,
    EventStatus Status,
    EventVisibility Visibility,
    SportDto Sport,
    EventVenueDto? Venue,
    string? CustomLocation,
    string? Title,
    DateTime StartsAt,
    DateTime EndsAt,
    string Timezone,
    int? MaxParticipants,
    int ConfirmedCount,
    decimal? Cost,
    string Currency);

public sealed record EventParticipantDto(
    Guid Id,
    Guid? UserId,
    string DisplayName,
    string? GuestName,
    ParticipationStatus Status,
    int? WaitlistOrder,
    DateTime JoinedAt);

public sealed record EventDetailDto(
    Guid Id,
    string PublicId,
    EventType Type,
    EventStatus Status,
    EventVisibility Visibility,
    SportDto Sport,
    Guid? ClubId,
    EventVenueDto? Venue,
    string? CustomLocation,
    string? Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    string Timezone,
    int? MinParticipants,
    int? MaxParticipants,
    bool WaitlistEnabled,
    bool RequiresApproval,
    SkillLevel? SkillLevelMin,
    SkillLevel? SkillLevelMax,
    GenderPolicy GenderPolicy,
    int? AgeMin,
    int? AgeMax,
    CostSplit CostSplit,
    decimal? Cost,
    string Currency,
    DateTime? RegistrationOpensAt,
    DateTime? RegistrationClosesAt,
    int? LockHoursBeforeStart,
    int ConfirmedCount,
    int MaybeCount,
    int WaitlistCount,
    Guid? CreatedById,
    DateTime? CancelledAt,
    string? CancelReason,
    List<EventParticipantDto> Participants,
    List<EventTeamDto> Teams,
    // Результаты (Шаг 14) — заполняется после EventStatus.Completed.
    EventResultDto? Result,
    List<MvpTallyEntryDto> MvpTally,
    Guid? MyMvpVote,
    // Только собственный платёж viewer'а (Шаг 18) — список чужих платежей не светим,
    // отдельный GET /api/events/{id}/payments для создателя/модератора.
    PaymentSummaryDto? MyPayment,
    // Резолвнутые Title/Description выше — для страницы просмотра. Эти два —
    // сырые словари по всем языкам, только для формы редактирования.
    LocalizedTextDto? TitleI18n,
    LocalizedTextDto? DescriptionI18n);

/// <summary>
/// Поля, которых нет в CreateEventRequest (SportId/ClubId/Type) —
/// осознанно неизменяемы после создания, как Slug у Venue.
/// </summary>
public sealed record UpdateEventRequest(
    EventVisibility? Visibility,
    Guid? VenueId,
    string? CustomLocation,
    LocalizedTextDto? Title,
    LocalizedTextDto? Description,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Timezone,
    int? MinParticipants,
    int? MaxParticipants,
    bool? WaitlistEnabled,
    bool? RequiresApproval,
    SkillLevel? SkillLevelMin,
    SkillLevel? SkillLevelMax,
    GenderPolicy? GenderPolicy,
    int? AgeMin,
    int? AgeMax,
    CostSplit? CostSplit,
    decimal? Cost,
    string? Currency,
    int? LockHoursBeforeStart);

public sealed record CreateEventRequest(
    EventType Type,
    EventVisibility Visibility,
    Guid SportId,
    Guid? ClubId,
    Guid? VenueId,
    string? CustomLocation,
    LocalizedTextDto? Title,
    LocalizedTextDto? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    string? Timezone,
    int? MinParticipants,
    int? MaxParticipants,
    bool? WaitlistEnabled,
    bool? RequiresApproval,
    SkillLevel? SkillLevelMin,
    SkillLevel? SkillLevelMax,
    GenderPolicy? GenderPolicy,
    int? AgeMin,
    int? AgeMax,
    CostSplit? CostSplit,
    decimal? Cost,
    string? Currency,
    int? LockHoursBeforeStart);

/// <summary>Status — только Confirmed или Maybe; остальное (Waitlisted, PendingApproval, Declined...) — считает сервер.</summary>
public sealed record JoinEventRequest(ParticipationStatus Status);

public sealed record AddGuestRequest(string GuestName);

public sealed record CancelEventRequest(string? Reason);

public sealed record EventJoinRequestDto(Guid ParticipantId, Guid UserId, string DisplayName, DateTime RequestedAt);
