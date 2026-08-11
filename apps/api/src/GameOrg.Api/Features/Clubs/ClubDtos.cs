using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Clubs;

public sealed record ClubDto(
    Guid Id,
    string Slug,
    string Name,
    CityDto? City,
    ClubVisibility Visibility,
    ClubKind Kind,
    string? AvatarUrl,
    int MembersCount,
    int EventsCount);

public sealed record ClubDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    CityDto? City,
    ClubVisibility Visibility,
    ClubKind Kind,
    string? AvatarUrl,
    int MembersCount,
    int EventsCount,
    Guid? CreatedById,
    // Только для Owner/Admin — иначе null, инвайт-код открывает вступление в Private-клуб.
    string? InviteCode,
    ClubRole? ViewerRole,
    MembershipStatus? ViewerStatus,
    int FollowersCount,
    bool ViewerIsFollowing,
    List<SportDto> Sports,
    // Резолвнутые Name/Description выше — для страницы просмотра. Эти два —
    // сырые словари по всем языкам, нужны только форме редактирования.
    LocalizedTextDto NameI18n,
    LocalizedTextDto? DescriptionI18n);

public sealed record ClubMemberDto(
    Guid UserId,
    string DisplayName,
    ClubRole Role,
    MembershipStatus Status,
    DateTime JoinedAt);

public sealed record CreateClubRequest(
    LocalizedTextDto Name,
    LocalizedTextDto? Description,
    Guid? CityId,
    ClubVisibility? Visibility,
    // Неизменяем после создания (см. Club.Kind) — при create отсутствует в UpdateClubRequest намеренно.
    ClubKind? Kind,
    List<Guid>? SportIds);

/// <summary>Поле есть в теле и не null → применяется в PATCH; отсутствует/null → не трогается.</summary>
public sealed record UpdateClubRequest(
    LocalizedTextDto? Name,
    LocalizedTextDto? Description,
    Guid? CityId,
    ClubVisibility? Visibility,
    List<Guid>? SportIds);

// Логотип — один MediaAsset на клуб (AvatarId), не галерея, как у Venue.Photos.
public sealed record PresignClubAvatarRequest(string ContentType);
public sealed record PresignClubAvatarResponse(Guid MediaId, string UploadUrl, string PublicUrl);
public sealed record AttachClubAvatarRequest(Guid MediaId);

public sealed record InviteMemberRequest(Guid UserId);

public sealed record SetMemberRoleRequest(ClubRole Role);
