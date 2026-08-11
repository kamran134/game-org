using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Moderation;
using GameOrg.Api.Features.Social;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GameOrg.Api.Features.Venues;

/// <summary>CRUD площадок, фото (через R2) и отзывы.</summary>
public sealed class VenueService(
    GameOrgDbContext db, R2StorageService storage, AuditLogService auditLog, FollowService followService, ActivityService activityService)
{
    public async Task<(Venue? Result, string? Error)> CreateAsync(Guid userId, UserRole creatorRole, CreateVenueRequest request, CancellationToken ct)
    {
        if (request.Name.IsEmpty) return (null, "Название: хотя бы один язык обязателен.");
        if (request.Name.ExceedsMaxLength(120)) return (null, "Название — до 120 символов на каждый язык.");
        if (request.Description?.ExceedsMaxLength(2000) == true) return (null, "Описание — до 2000 символов на каждый язык.");
        if (request.Address?.ExceedsMaxLength(300) == true) return (null, "Адрес — до 300 символов на каждый язык.");

        var nameI18n = request.Name.ToDict()!; // не null — IsEmpty уже проверили выше
        var venue = new Venue
        {
            // Слаг — из первого непустого имени по фолбэку az→en→ru, не из
            // языка запроса: один и тот же слаг должен получаться независимо
            // от того, на каком языке заполняли форму (см. docs/PLAN.md, Шаг 7.5).
            Slug = await GenerateUniqueSlugAsync(Localized.Resolve(nameI18n, RequestLocale.Default) ?? "", ct),
            NameI18n = nameI18n,
            DescriptionI18n = request.Description?.ToDict(),
            AddressI18n = request.Address?.ToDict(),
            CityId = request.CityId,
            Location = new Point(request.Lng, request.Lat) { SRID = 4326 },
            IsIndoor = request.IsIndoor,
            Surface = request.Surface,
            HasLighting = request.HasLighting ?? false,
            HasShowers = request.HasShowers ?? false,
            HasParking = request.HasParking ?? false,
            HasTribunes = request.HasTribunes ?? false,
            PriceHint = request.PriceHint,
            Currency = request.Currency ?? "AZN",
            Phone = request.Phone,
            Website = request.Website,
            CreatedById = userId,
            // Премодерация (docs/PLAN.md, Шаг 9): обычный пользователь публикует
            // в общий каталог не сразу, модератор/админ — сразу, иначе плодили
            // бы очередь себе же.
            Status = creatorRole == UserRole.User ? VenueStatus.Draft : VenueStatus.Published,
        };

        if (request.SportIds is { Count: > 0 })
        {
            foreach (var sportId in request.SportIds.Distinct())
                venue.Sports.Add(new VenueSport { SportId = sportId });
        }

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);
        return (venue, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid venueId, Guid userId, UpdateVenueRequest request, CancellationToken ct)
    {
        var venue = await db.Venues.Include(v => v.Sports).FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return (false, "Площадка не найдена.");
        if (venue.CreatedById != userId) return (false, "Редактировать может только создатель.");

        // Слаг НЕ трогаем при редактировании, даже если название целиком поменялось —
        // ломать существующие ссылки на площадку нельзя.
        if (request.Name is not null)
        {
            if (request.Name.ExceedsMaxLength(120)) return (false, "Название — до 120 символов на каждый язык.");
            var nameDict = request.Name.ToDict();
            if (nameDict is null) return (false, "Название: хотя бы один язык обязателен.");
            venue.NameI18n = nameDict;
        }
        if (request.Description is not null)
        {
            if (request.Description.ExceedsMaxLength(2000)) return (false, "Описание — до 2000 символов на каждый язык.");
            venue.DescriptionI18n = request.Description.ToDict();
        }
        if (request.Address is not null)
        {
            if (request.Address.ExceedsMaxLength(300)) return (false, "Адрес — до 300 символов на каждый язык.");
            venue.AddressI18n = request.Address.ToDict();
        }
        if (request.CityId is not null) venue.CityId = request.CityId;
        if (request.Lat is not null && request.Lng is not null)
            venue.Location = new Point(request.Lng.Value, request.Lat.Value) { SRID = 4326 };
        if (request.IsIndoor is not null) venue.IsIndoor = request.IsIndoor;
        if (request.Surface is not null) venue.Surface = request.Surface;
        if (request.HasLighting is not null) venue.HasLighting = request.HasLighting.Value;
        if (request.HasShowers is not null) venue.HasShowers = request.HasShowers.Value;
        if (request.HasParking is not null) venue.HasParking = request.HasParking.Value;
        if (request.HasTribunes is not null) venue.HasTribunes = request.HasTribunes.Value;
        if (request.PriceHint is not null) venue.PriceHint = request.PriceHint;
        if (request.Currency is not null) venue.Currency = request.Currency;
        if (request.Phone is not null) venue.Phone = request.Phone.Length == 0 ? null : request.Phone;
        if (request.Website is not null) venue.Website = request.Website.Length == 0 ? null : request.Website;

        if (request.SportIds is not null)
        {
            venue.Sports.Clear();
            foreach (var sportId in request.SportIds.Distinct())
                venue.Sports.Add(new VenueSport { SportId = sportId, VenueId = venue.Id });
        }

        venue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// viewerId/viewerIsModerator — Draft видят только автор и модератор,
    /// остальным как будто площадки не существует (404, не 403 — не
    /// подтверждаем даже сам факт существования чужого черновика).
    /// </summary>
    /// <summary>Автор или модератор. Soft-delete — DeletedAt, глобальный HasQueryFilter уже прячет такие строки из всех выдач.</summary>
    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid venueId, Guid userId, bool isModerator, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return (false, "Площадка не найдена.");
        if (venue.CreatedById != userId && !isModerator) return (false, "Удалить может только автор или модератор.");

        venue.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(userId, "venue.delete", nameof(Venue), venue.Id, null, null, ct);
        return (true, null);
    }

    public async Task<VenueDetailDto?> GetBySlugAsync(string slug, string locale, Guid? viewerId, bool viewerIsModerator, CancellationToken ct)
    {
        var venue = await db.Venues
            .Include(v => v.City)
            .Include(v => v.Sports).ThenInclude(s => s.Sport)
            .Include(v => v.Photos).ThenInclude(p => p.Media)
            .FirstOrDefaultAsync(v => v.Slug == slug, ct);

        if (venue is null) return null;
        if (venue.Status == VenueStatus.Draft && venue.CreatedById != viewerId && !viewerIsModerator) return null;

        var followersCount = await followService.CountFollowersAsync(FollowTargetType.Venue, venue.Id, ct);
        var viewerIsFollowing = await followService.IsFollowingAsync(viewerId, FollowTargetType.Venue, venue.Id, ct);
        return MapDetail(venue, locale, followersCount, viewerIsFollowing);
    }

    /// <summary>Только модератор/админ — очередь площадок, ожидающих премодерации.</summary>
    public async Task<List<VenueDto>> GetModerationQueueAsync(string locale, CancellationToken ct)
    {
        var venues = await db.Venues
            .Where(v => v.Status == VenueStatus.Draft)
            .OrderBy(v => v.CreatedAt)
            .Take(50)
            .Select(v => new { v.Id, v.Slug, v.NameI18n, v.AddressI18n, v.Location, v.RatingAvg, v.RatingCount })
            .ToListAsync(ct);

        return venues.Select(v => new VenueDto(
            v.Id, v.Slug,
            Localized.Resolve(v.NameI18n, locale) ?? "",
            Localized.Resolve(v.AddressI18n, locale),
            v.Location.Y, v.Location.X,
            v.RatingAvg, v.RatingCount, null)).ToList();
    }

    /// <summary>Публикация/скрытие модератором — только эти два статуса, Draft/Merged через этот путь не выставляются.</summary>
    public async Task<(bool Ok, string? Error)> SetStatusAsync(Guid venueId, Guid actorId, VenueStatus status, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return (false, "Площадка не найдена.");

        var before = venue.Status;
        venue.Status = status;
        venue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(
            actorId, status == VenueStatus.Published ? "venue.publish" : "venue.hide", nameof(Venue), venue.Id,
            new { Status = before }, new { Status = status }, ct);
        return (true, null);
    }

    /// <summary>Заявка «это моя площадка» — 409, если пара Venue+User уже есть (unique-индекс это и так не
    /// пустит, тут просто понятная ошибка вместо 500).</summary>
    public async Task<(VenueClaim? Result, string? Error)> CreateClaimAsync(Guid venueId, Guid userId, CreateVenueClaimRequest request, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return (null, "Площадка не найдена.");
        if (venue.CreatedById == userId) return (null, "Вы уже владелец этой площадки.");
        if (await db.VenueClaims.AnyAsync(c => c.VenueId == venueId && c.UserId == userId, ct))
            return (null, "Вы уже подавали заявку на эту площадку.");

        var claim = new VenueClaim { VenueId = venueId, UserId = userId, Evidence = request.Evidence };
        db.VenueClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        return (claim, null);
    }

    /// <summary>Только модератор/админ — очередь заявок на владение площадками.</summary>
    public async Task<List<VenueClaimDto>> GetClaimQueueAsync(string locale, CancellationToken ct)
    {
        var claims = await db.VenueClaims
            .Where(c => c.Status == ReportStatus.Open)
            .Include(c => c.Venue)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return claims.Select(c => new VenueClaimDto(
            c.Id, c.VenueId, Localized.Resolve(c.Venue.NameI18n, locale) ?? c.Venue.Slug,
            c.UserId, Localized.Resolve(c.User.DisplayNameI18n, locale) ?? c.User.Handle,
            c.Evidence, c.Status, c.CreatedAt)).ToList();
    }

    /// <summary>Одобрение переносит владение: Venue.CreatedById = claim.UserId (решение пользователя, Шаг 10).</summary>
    public async Task<(bool Ok, string? Error)> ApproveClaimAsync(Guid claimId, Guid resolverId, CancellationToken ct)
    {
        var claim = await db.VenueClaims.Include(c => c.Venue).FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return (false, "Заявка не найдена.");
        if (claim.Status != ReportStatus.Open) return (false, "Заявка уже обработана.");

        claim.Status = ReportStatus.Resolved;
        claim.ResolvedById = resolverId;
        claim.ResolvedAt = DateTime.UtcNow;
        var previousOwnerId = claim.Venue.CreatedById;
        claim.Venue.CreatedById = claim.UserId;
        claim.Venue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(
            resolverId, "venueclaim.approve", nameof(VenueClaim), claim.Id,
            new { CreatedById = previousOwnerId }, new { CreatedById = claim.UserId }, ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RejectClaimAsync(Guid claimId, Guid resolverId, CancellationToken ct)
    {
        var claim = await db.VenueClaims.FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return (false, "Заявка не найдена.");
        if (claim.Status != ReportStatus.Open) return (false, "Заявка уже обработана.");

        claim.Status = ReportStatus.Rejected;
        claim.ResolvedById = resolverId;
        claim.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(resolverId, "venueclaim.reject", nameof(VenueClaim), claim.Id, null, null, ct);
        return (true, null);
    }

    private VenueDetailDto MapDetail(Venue venue, string locale, int followersCount, bool viewerIsFollowing) => new(
        venue.Id, venue.Slug,
        Localized.Resolve(venue.NameI18n, locale) ?? "",
        Localized.Resolve(venue.DescriptionI18n, locale),
        Localized.Resolve(venue.AddressI18n, locale),
        venue.City is null ? null : new CityDto(venue.City.Id, venue.City.Slug, venue.City.NameI18n, venue.City.Lat, venue.City.Lng),
        venue.Location.Y, venue.Location.X,
        venue.IsIndoor, venue.Surface,
        venue.HasLighting, venue.HasShowers, venue.HasParking, venue.HasTribunes,
        venue.PriceHint, venue.Currency, venue.Phone, venue.Website, venue.OpeningHours,
        venue.RatingAvg, venue.RatingCount, venue.EventsCount, venue.CreatedById, venue.Status,
        followersCount, viewerIsFollowing,
        venue.Sports.Select(s => new VenueSportDto(
            new SportDto(s.Sport.Id, s.Sport.Slug, s.Sport.NameI18n, s.Sport.Emoji, s.Sport.HasPositions, s.Sport.IsTeamSport),
            s.Courts)).ToList(),
        venue.Photos.OrderBy(p => p.SortOrder).Select(p => new VenuePhotoDto(
            p.Id, storage.GetPublicUrl(p.Media.BucketKey), p.IsCover, p.SortOrder)).ToList(),
        LocalizedTextDto.From(venue.NameI18n),
        LocalizedTextDto.FromNullable(venue.DescriptionI18n),
        LocalizedTextDto.FromNullable(venue.AddressI18n));

    public async Task<(PresignPhotoResponse? Result, string? Error)> PresignPhotoAsync(
        Guid venueId, Guid userId, PresignPhotoRequest request, CancellationToken ct)
    {
        if (!storage.IsConfigured)
            return (null, "Загрузка медиа пока не настроена на сервере.");

        var venueExists = await db.Venues.AnyAsync(v => v.Id == venueId, ct);
        if (!venueExists) return (null, "Площадка не найдена.");

        if (!request.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return (null, "Разрешены только изображения.");

        var extension = request.ContentType.Split('/') is [_, var ext] ? ext : "bin";
        var key = $"venues/{venueId}/{Guid.CreateVersion7()}.{extension}";

        var media = new MediaAsset
        {
            OwnerId = userId,
            Kind = MediaKind.Image,
            BucketKey = key,
            MimeType = request.ContentType,
            SizeBytes = 0,
        };
        db.MediaAssets.Add(media);
        await db.SaveChangesAsync(ct);

        var uploadUrl = await storage.GetPresignedUploadUrlAsync(key, request.ContentType, TimeSpan.FromMinutes(15));
        return (new PresignPhotoResponse(media.Id, uploadUrl, storage.GetPublicUrl(key)), null);
    }

    public async Task<(VenuePhotoDto? Result, string? Error)> AttachPhotoAsync(
        Guid venueId, Guid userId, AttachPhotoRequest request, CancellationToken ct)
    {
        var venueExists = await db.Venues.AnyAsync(v => v.Id == venueId, ct);
        if (!venueExists) return (null, "Площадка не найдена.");

        var media = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == request.MediaId && m.OwnerId == userId, ct);
        if (media is null) return (null, "Медиа не найдено.");

        if (request.SizeBytes is not null) media.SizeBytes = request.SizeBytes.Value;
        if (request.Width is not null) media.Width = request.Width;
        if (request.Height is not null) media.Height = request.Height;

        if (request.IsCover == true)
        {
            var previousCover = await db.VenuePhotos.FirstOrDefaultAsync(p => p.VenueId == venueId && p.IsCover, ct);
            if (previousCover is not null)
            {
                // Отдельный SaveChanges до вставки новой обложки — та же причина,
                // что с is_primary в ProfileService: partial unique index
                // venue_photos_cover_uq не переживёт две true-строки одновременно.
                previousCover.IsCover = false;
                await db.SaveChangesAsync(ct);
            }
        }

        var sortOrder = await db.VenuePhotos.Where(p => p.VenueId == venueId).CountAsync(ct);
        var photo = new VenuePhoto
        {
            VenueId = venueId,
            MediaId = media.Id,
            IsCover = request.IsCover ?? false,
            SortOrder = sortOrder,
        };
        db.VenuePhotos.Add(photo);
        await db.SaveChangesAsync(ct);

        return (new VenuePhotoDto(photo.Id, storage.GetPublicUrl(media.BucketKey), photo.IsCover, photo.SortOrder), null);
    }

    public async Task<bool> RemovePhotoAsync(Guid venueId, Guid photoId, Guid userId, CancellationToken ct)
    {
        var photo = await db.VenuePhotos
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.VenueId == venueId, ct);
        if (photo is null) return false;

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        var canRemove = photo.Media.OwnerId == userId || venue?.CreatedById == userId;
        if (!canRemove) return false;

        db.VenuePhotos.Remove(photo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<VenueReviewDto>> GetReviewsAsync(Guid venueId, int skip, int take, string locale, CancellationToken ct)
    {
        // Localized.Resolve — обычный C#-метод, EF не умеет транслировать его
        // в SQL, поэтому сначала материализуем сырые DisplayNameI18n, резолвим
        // в памяти — тот же приём, что с Location.X/Y в GET /api/venues.
        var raw = await db.VenueReviews
            .Where(r => r.VenueId == venueId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .Select(r => new { r.Id, r.AuthorId, r.Author.DisplayNameI18n, r.Rating, r.Text, r.CreatedAt, r.UpdatedAt })
            .ToListAsync(ct);

        return raw.Select(r => new VenueReviewDto(
            r.Id, r.AuthorId, Localized.Resolve(r.DisplayNameI18n, locale) ?? "", r.Rating, r.Text, r.CreatedAt, r.UpdatedAt)).ToList();
    }

    public async Task<(VenueReviewDto? Result, string? Error, bool Conflict)> CreateReviewAsync(
        Guid venueId, Guid userId, UpsertReviewRequest request, string locale, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
            return (null, "Оценка должна быть от 1 до 5.", false);

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return (null, "Площадка не найдена.", false);

        var alreadyReviewed = await db.VenueReviews.AnyAsync(r => r.VenueId == venueId && r.AuthorId == userId, ct);
        if (alreadyReviewed) return (null, "Вы уже оставляли отзыв на эту площадку.", true);

        var review = new VenueReview { VenueId = venueId, AuthorId = userId, Rating = request.Rating, Text = request.Text };
        db.VenueReviews.Add(review);
        await db.SaveChangesAsync(ct);
        await RecomputeRatingAsync(venueId, ct);

        if (venue.Status == VenueStatus.Published)
            await activityService.EmitAsync(userId, ActivityVerb.ReviewedVenue, null, null, venueId, null, ct);

        var author = await db.Users.FirstAsync(u => u.Id == userId, ct);
        return (new VenueReviewDto(review.Id, userId, Localized.Resolve(author.DisplayNameI18n, locale) ?? "", review.Rating, review.Text, review.CreatedAt, review.UpdatedAt), null, false);
    }

    public async Task<(VenueReviewDto? Result, string? Error)> UpdateReviewAsync(
        Guid venueId, Guid reviewId, Guid userId, UpsertReviewRequest request, string locale, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
            return (null, "Оценка должна быть от 1 до 5.");

        var review = await db.VenueReviews
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.VenueId == venueId, ct);
        if (review is null) return (null, "Отзыв не найден.");
        if (review.AuthorId != userId) return (null, "Редактировать можно только свой отзыв.");

        review.Rating = request.Rating;
        review.Text = request.Text;
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeRatingAsync(venueId, ct);

        return (new VenueReviewDto(review.Id, review.AuthorId, Localized.Resolve(review.Author.DisplayNameI18n, locale) ?? "", review.Rating, review.Text, review.CreatedAt, review.UpdatedAt), null);
    }

    public async Task<bool> RemoveReviewAsync(Guid venueId, Guid reviewId, Guid userId, CancellationToken ct)
    {
        var review = await db.VenueReviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.VenueId == venueId && r.AuthorId == userId, ct);
        if (review is null) return false;

        db.VenueReviews.Remove(review);
        await db.SaveChangesAsync(ct);
        await RecomputeRatingAsync(venueId, ct);
        return true;
    }

    private async Task RecomputeRatingAsync(Guid venueId, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue is null) return;

        var reviews = db.VenueReviews.Where(r => r.VenueId == venueId);
        venue.RatingCount = await reviews.CountAsync(ct);
        venue.RatingAvg = venue.RatingCount == 0 ? null : (decimal)await reviews.AverageAsync(r => r.Rating, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = VenueSlugGenerator.Generate(name);
            var taken = await db.Venues.AnyAsync(v => v.Slug == candidate, ct);
            if (!taken) return candidate;
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный слаг после 5 попыток.");
    }
}
