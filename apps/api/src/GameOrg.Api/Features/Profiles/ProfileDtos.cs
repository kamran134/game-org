using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Profiles;

public sealed record UserSportPositionDto(Guid PositionId, string Code, bool IsPrimary);

public sealed record UserSportDto(
    Guid SportId,
    string SportSlug,
    string? SportEmoji,
    SkillLevel Level,
    bool IsPrimary,
    int? PlayingSince,
    Footedness? Footedness,
    int? HeightCm,
    int? JerseyNumber,
    string? Note,
    Visibility Visibility,
    List<UserSportPositionDto> Positions);

public sealed record MeProfileDto(
    Guid Id,
    string Handle,
    string DisplayName,
    string? Bio,
    DateOnly? BirthDate,
    Gender? Gender,
    string? Phone,
    string Locale,
    string Timezone,
    Visibility ProfileVisibility,
    CityDto? City,
    Guid? AvatarId,
    bool IsVerified,
    UserRole Role,
    List<UserSportDto> Sports,
    // Резолвнутые DisplayName/Bio выше — для отображения. Эти два — сырые
    // словари по всем языкам, нужны только форме редактирования (/me).
    LocalizedTextDto DisplayNameI18n,
    LocalizedTextDto? BioI18n);

public sealed record PublicUserSportDto(
    string SportSlug,
    string? SportEmoji,
    SkillLevel Level,
    bool IsPrimary,
    int? PlayingSince,
    Footedness? Footedness,
    int? HeightCm,
    int? JerseyNumber,
    List<UserSportPositionDto> Positions);

public sealed record PublicProfileDto(
    string Handle,
    string DisplayName,
    string? Bio,
    CityDto? City,
    Guid? AvatarId,
    bool IsVerified,
    List<PublicUserSportDto> Sports);

/// <summary>
/// Поле есть в теле и не null → применяется. Поле отсутствует/null → не
/// трогается. Очистка строкового поля — прислать "". DisplayName/Bio —
/// присланный LocalizedTextDto целиком заменяет словарь (Bio можно свести к
/// null всеми пустыми языками, DisplayName — нет, нужен хотя бы один).
/// Nullable-value-type поля (CityId/Gender/BirthDate) в v1 нельзя явно
/// сбросить обратно в null.
/// </summary>
public sealed record UpdateMeRequest(
    LocalizedTextDto? DisplayName,
    LocalizedTextDto? Bio,
    string? Phone,
    string? Locale,
    string? Timezone,
    Guid? CityId,
    Visibility? ProfileVisibility,
    string? Handle,
    DateOnly? BirthDate,
    Gender? Gender);

public sealed record UpsertUserSportRequest(
    SkillLevel Level,
    bool IsPrimary,
    int? PlayingSince,
    Footedness? Footedness,
    int? HeightCm,
    int? JerseyNumber,
    string? Note,
    Visibility Visibility,
    List<Guid> PositionIds,
    Guid? PrimaryPositionId);
