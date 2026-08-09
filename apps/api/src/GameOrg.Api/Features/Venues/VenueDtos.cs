using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Venues;

public sealed record VenueDto(
    Guid Id,
    string Slug,
    string Name,
    string? Address,
    double Lat,
    double Lng,
    decimal? RatingAvg,
    int RatingCount,
    double? DistanceMeters);

public sealed record VenueSportDto(SportDto Sport, int Courts);

public sealed record VenuePhotoDto(Guid Id, string Url, bool IsCover, int SortOrder);

public sealed record VenueReviewDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    int Rating,
    string? Text,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record VenueDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? Address,
    CityDto? City,
    double Lat,
    double Lng,
    bool? IsIndoor,
    VenueSurface? Surface,
    bool HasLighting,
    bool HasShowers,
    bool HasParking,
    bool HasTribunes,
    decimal? PriceHint,
    string Currency,
    string? Phone,
    string? Website,
    Dictionary<string, object>? OpeningHours,
    decimal? RatingAvg,
    int RatingCount,
    int EventsCount,
    Guid? CreatedById,
    VenueStatus Status,
    List<VenueSportDto> Sports,
    List<VenuePhotoDto> Photos,
    // Резолвнутые Name/Description/Address выше — для страницы просмотра.
    // Эти три — сырые словари по всем языкам, нужны только форме редактирования.
    LocalizedTextDto NameI18n,
    LocalizedTextDto? DescriptionI18n,
    LocalizedTextDto? AddressI18n);

/// <summary>Поле есть в теле и не null → применяется в PATCH; отсутствует/null → не трогается.</summary>
public sealed record UpdateVenueRequest(
    LocalizedTextDto? Name,
    LocalizedTextDto? Description,
    LocalizedTextDto? Address,
    Guid? CityId,
    double? Lat,
    double? Lng,
    bool? IsIndoor,
    VenueSurface? Surface,
    bool? HasLighting,
    bool? HasShowers,
    bool? HasParking,
    bool? HasTribunes,
    decimal? PriceHint,
    string? Currency,
    string? Phone,
    string? Website,
    List<Guid>? SportIds);

public sealed record CreateVenueRequest(
    LocalizedTextDto Name,
    LocalizedTextDto? Description,
    LocalizedTextDto? Address,
    Guid? CityId,
    double Lat,
    double Lng,
    bool? IsIndoor,
    VenueSurface? Surface,
    bool? HasLighting,
    bool? HasShowers,
    bool? HasParking,
    bool? HasTribunes,
    decimal? PriceHint,
    string? Currency,
    string? Phone,
    string? Website,
    List<Guid>? SportIds);

public sealed record PresignPhotoRequest(string ContentType);

public sealed record PresignPhotoResponse(Guid MediaId, string UploadUrl, string PublicUrl);

public sealed record AttachPhotoRequest(Guid MediaId, bool? IsCover, int? SizeBytes, int? Width, int? Height);

public sealed record UpsertReviewRequest(int Rating, string? Text);
