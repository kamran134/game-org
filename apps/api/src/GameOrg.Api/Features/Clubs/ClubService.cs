using System.Security.Cryptography;
using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Social;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using GameOrg.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Clubs;

/// <summary>CRUD клубов и управление участниками. Group (Шаг 20) — не отдельная сущность, а Club.Kind.</summary>
public sealed class ClubService(
    GameOrgDbContext db, NotificationSender notificationSender, FollowService followService,
    ActivityService activityService, R2StorageService storage)
{
    public async Task<(Club? Result, string? Error)> CreateAsync(Guid userId, CreateClubRequest request, CancellationToken ct)
    {
        if (request.Name.IsEmpty) return (null, "Название: хотя бы один язык обязателен.");
        if (request.Name.ExceedsMaxLength(80)) return (null, "Название — до 80 символов на каждый язык.");
        if (request.Description?.ExceedsMaxLength(1000) == true) return (null, "Описание — до 1000 символов на каждый язык.");

        var nameI18n = request.Name.ToDict()!; // не null — IsEmpty уже проверили выше
        var club = new Club
        {
            Slug = await GenerateUniqueSlugAsync(Localized.Resolve(nameI18n, RequestLocale.Default) ?? "", ct),
            NameI18n = nameI18n,
            DescriptionI18n = request.Description?.ToDict(),
            CityId = request.CityId,
            Visibility = request.Visibility ?? ClubVisibility.Public,
            Kind = request.Kind ?? ClubKind.Club,
            CreatedById = userId,
            MembersCount = 1,
        };

        if (request.SportIds is { Count: > 0 })
        {
            foreach (var sportId in request.SportIds.Distinct())
                club.Sports.Add(new ClubSport { SportId = sportId });
        }

        // Создатель сразу становится Owner — та же транзакция, что создание клуба.
        club.Members.Add(new ClubMember { UserId = userId, Role = ClubRole.Owner, Status = MembershipStatus.Active });

        db.Clubs.Add(club);
        await db.SaveChangesAsync(ct);

        if (club.Visibility != ClubVisibility.Private)
            await activityService.EmitAsync(userId, ActivityVerb.CreatedClub, null, club.Id, null, null, ct);

        return (club, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid clubId, Guid userId, UpdateClubRequest request, CancellationToken ct)
    {
        var club = await db.Clubs.Include(c => c.Sports).FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");
        if (!await IsOwnerOrAdminAsync(clubId, userId, ct)) return (false, "Редактировать может только владелец или админ клуба.");

        // Слаг не трогаем — ссылки на клуб не должны ломаться.
        if (request.Name is not null)
        {
            if (request.Name.ExceedsMaxLength(80)) return (false, "Название — до 80 символов на каждый язык.");
            var nameDict = request.Name.ToDict();
            if (nameDict is null) return (false, "Название: хотя бы один язык обязателен.");
            club.NameI18n = nameDict;
        }
        if (request.Description is not null)
        {
            if (request.Description.ExceedsMaxLength(1000)) return (false, "Описание — до 1000 символов на каждый язык.");
            club.DescriptionI18n = request.Description.ToDict();
        }
        if (request.CityId is not null) club.CityId = request.CityId;
        if (request.Visibility is not null) club.Visibility = request.Visibility.Value;

        if (request.SportIds is not null)
        {
            club.Sports.Clear();
            foreach (var sportId in request.SportIds.Distinct())
                club.Sports.Add(new ClubSport { SportId = sportId, ClubId = club.Id });
        }

        club.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Private невиден не-участникам — 404, не 403 (не подтверждаем сам факт существования).
    /// Public/RequestOnly видны всем, разница только в том, как в них вступить.
    /// </summary>
    public async Task<ClubDetailDto?> GetBySlugAsync(string slug, string locale, Guid? viewerId, CancellationToken ct)
    {
        // Notification.Data хранит внутренний Guid клуба, а не Slug (Шаг 22) —
        // ссылка из уведомления подставляет его сюда же, поэтому лукап понимает оба.
        var byId = Guid.TryParse(slug, out var clubId);
        var query = db.Clubs
            .Include(c => c.City)
            .Include(c => c.Avatar)
            .Include(c => c.Sports).ThenInclude(s => s.Sport)
            .AsQueryable();
        var club = byId
            ? await query.FirstOrDefaultAsync(c => c.Id == clubId, ct)
            : await query.FirstOrDefaultAsync(c => c.Slug == slug, ct);

        if (club is null) return null;

        ClubMember? viewerMembership = viewerId is null
            ? null
            : await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == club.Id && m.UserId == viewerId, ct);

        var viewerIsActive = viewerMembership is { Status: MembershipStatus.Active };
        if (club.Visibility == ClubVisibility.Private && !viewerIsActive) return null;

        var followersCount = await followService.CountFollowersAsync(FollowTargetType.Club, club.Id, ct);
        var viewerIsFollowing = await followService.IsFollowingAsync(viewerId, FollowTargetType.Club, club.Id, ct);
        return MapDetail(club, locale, viewerMembership, followersCount, viewerIsFollowing);
    }

    public async Task<List<ClubDto>> GetListAsync(
        string locale, Guid? viewerId, Guid? cityId, Guid? sportId, ClubKind? kind, bool onlyMine, CancellationToken ct)
    {
        // Private — только клубы, где viewer активный участник; остальным как будто их нет в каталоге.
        var viewerClubIds = viewerId is null
            ? []
            : await db.ClubMembers
                .Where(m => m.UserId == viewerId && m.Status == MembershipStatus.Active)
                .Select(m => m.ClubId)
                .ToListAsync(ct);

        if (onlyMine && viewerId is null) return [];

        var query = onlyMine
            ? db.Clubs.Where(c => viewerClubIds.Contains(c.Id) || c.CreatedById == viewerId)
            : db.Clubs.Where(c => c.Visibility != ClubVisibility.Private || viewerClubIds.Contains(c.Id));
        if (cityId is not null) query = query.Where(c => c.CityId == cityId);
        if (sportId is not null) query = query.Where(c => c.Sports.Any(s => s.SportId == sportId));
        if (kind is not null) query = query.Where(c => c.Kind == kind);

        var clubs = await query
            .Include(c => c.City)
            .Include(c => c.Avatar)
            .OrderByDescending(c => c.MembersCount)
            .Take(50)
            .ToListAsync(ct);

        return clubs.Select(MapListItem(locale)).ToList();
    }

    /// <summary>Клубы, где viewer активный участник — для селектора клуба в форме события (ClubId требует активного членства).</summary>
    public async Task<List<ClubDto>> GetMyClubsAsync(Guid userId, string locale, CancellationToken ct)
    {
        var clubs = await db.ClubMembers
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .Select(m => m.Club)
            .Include(c => c.City)
            .Include(c => c.Avatar)
            .ToListAsync(ct);

        return clubs.Select(MapListItem(locale)).ToList();
    }

    private Func<Club, ClubDto> MapListItem(string locale) => c => new ClubDto(
        c.Id, c.Slug, Localized.Resolve(c.NameI18n, locale) ?? "",
        MapCity(c.City), c.Visibility, c.Kind, c.Avatar is null ? null : storage.GetPublicUrl(c.Avatar.BucketKey),
        c.MembersCount, c.EventsCount);

    /// <summary>Только Owner. Soft-delete — DeletedAt, глобальный HasQueryFilter уже прячет клуб из всех выдач.</summary>
    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");

        var membership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        if (membership is not { Role: ClubRole.Owner, Status: MembershipStatus.Active })
            return (false, "Удалить клуб может только владелец.");

        club.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>Public — сразу Active; RequestOnly — Pending, ждёт Owner/Admin; Private — вообще не отсюда (только InviteAsync).</summary>
    public async Task<(bool Ok, string? Error)> JoinAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");
        if (club.Visibility == ClubVisibility.Private) return (false, "В этот клуб можно попасть только по приглашению.");

        var existing = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        if (existing is { Status: MembershipStatus.Active }) return (false, "Вы уже участник этого клуба.");
        if (existing is { Status: MembershipStatus.Pending }) return (false, "Заявка уже отправлена, ждите одобрения.");
        if (existing is { Status: MembershipStatus.Banned }) return (false, "Вы забанены в этом клубе.");

        var status = club.Visibility == ClubVisibility.Public ? MembershipStatus.Active : MembershipStatus.Pending;

        if (existing is null)
        {
            db.ClubMembers.Add(new ClubMember { ClubId = clubId, UserId = userId, Status = status });
        }
        else
        {
            // Left ранее — разрешаем повторное вступление, сбрасываем статус.
            existing.Status = status;
            existing.Role = ClubRole.Member;
            existing.JoinedAt = DateTime.UtcNow;
            existing.LeftAt = null;
        }

        await db.SaveChangesAsync(ct);

        if (status == MembershipStatus.Active)
        {
            await RecomputeMembersCountAsync(clubId, ct);
            await activityService.EmitAsync(userId, ActivityVerb.JoinedClub, null, clubId, null, null, ct);
        }
        else
        {
            var managerIds = await db.ClubMembers
                .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Active && (m.Role == ClubRole.Owner || m.Role == ClubRole.Admin))
                .Select(m => m.UserId)
                .ToListAsync(ct);

            foreach (var managerId in managerIds)
            {
                await notificationSender.SendAsync(
                    managerId, NotificationType.ClubJoinRequest,
                    $"CLUB_JOIN_REQUEST:{clubId}:{userId}:{DateTime.UtcNow.Ticks}",
                    "Новая заявка на вступление в клуб.",
                    new Dictionary<string, object> { ["clubId"] = clubId.ToString() }, ct);
            }
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> LeaveAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Active, ct);
        if (member is null) return (false, "Вы не участник этого клуба.");
        if (member.Role == ClubRole.Owner) return (false, "Владелец сначала должен передать права другому участнику.");

        member.Status = MembershipStatus.Left;
        member.LeftAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeMembersCountAsync(clubId, ct);
        return (true, null);
    }

    /// <summary>Owner/Admin добавляет конкретного пользователя сразу как Active — единственный путь в Private-клуб.</summary>
    public async Task<(bool Ok, string? Error)> InviteAsync(Guid clubId, Guid actorId, Guid targetUserId, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, actorId, ct)) return (false, "Приглашать может только владелец или админ клуба.");

        var existing = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId, ct);
        if (existing is { Status: MembershipStatus.Active }) return (false, "Пользователь уже в клубе.");

        if (existing is null)
        {
            db.ClubMembers.Add(new ClubMember { ClubId = clubId, UserId = targetUserId, Status = MembershipStatus.Active, InvitedById = actorId });
        }
        else
        {
            // Приглашение снимает и Banned — явное решение админа перевешивает прошлый бан.
            existing.Status = MembershipStatus.Active;
            existing.Role = ClubRole.Member;
            existing.InvitedById = actorId;
            existing.JoinedAt = DateTime.UtcNow;
            existing.LeftAt = null;
        }

        await db.SaveChangesAsync(ct);
        await RecomputeMembersCountAsync(clubId, ct);
        await notificationSender.SendAsync(
            targetUserId, NotificationType.ClubInvite,
            $"CLUB_INVITE:{clubId}:{targetUserId}:{DateTime.UtcNow.Ticks}",
            "Вас добавили в клуб.",
            new Dictionary<string, object> { ["clubId"] = clubId.ToString() }, ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ApproveJoinRequestAsync(Guid clubId, Guid actorId, Guid targetUserId, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, actorId, ct)) return (false, "Одобрять заявки может только владелец или админ клуба.");

        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId && m.Status == MembershipStatus.Pending, ct);
        if (member is null) return (false, "Заявка не найдена.");

        member.Status = MembershipStatus.Active;
        member.JoinedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeMembersCountAsync(clubId, ct);
        // RequestOnly — единственный путь сюда (Public вступает сразу через JoinAsync,
        // Private вообще не имеет заявок), значит Visibility != Private гарантирован.
        await activityService.EmitAsync(targetUserId, ActivityVerb.JoinedClub, null, clubId, null, null, ct);
        return (true, null);
    }

    /// <summary>Удаляет саму заявку (не Rejected-статус — его нет в MembershipStatus), можно попробовать вступить снова.</summary>
    public async Task<(bool Ok, string? Error)> RejectJoinRequestAsync(Guid clubId, Guid actorId, Guid targetUserId, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, actorId, ct)) return (false, "Отклонять заявки может только владелец или админ клуба.");

        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId && m.Status == MembershipStatus.Pending, ct);
        if (member is null) return (false, "Заявка не найдена.");

        db.ClubMembers.Remove(member);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>Owner/Admin — Owner'а удалить нельзя (сначала передать владение), Admin'а — только Owner.</summary>
    public async Task<(bool Ok, string? Error)> RemoveMemberAsync(Guid clubId, Guid actorId, Guid targetUserId, CancellationToken ct)
    {
        var actorMembership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == actorId, ct);
        if (actorMembership is not { Status: MembershipStatus.Active } || actorMembership.Role is not (ClubRole.Owner or ClubRole.Admin))
            return (false, "Удалять участников может только владелец или админ клуба.");

        var target = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId && m.Status == MembershipStatus.Active, ct);
        if (target is null) return (false, "Участник не найден.");
        if (target.Role == ClubRole.Owner) return (false, "Владельца удалить нельзя.");
        if (target.Role == ClubRole.Admin && actorMembership.Role != ClubRole.Owner) return (false, "Админа может удалить только владелец.");

        target.Status = MembershipStatus.Banned;
        target.LeftAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeMembersCountAsync(clubId, ct);
        return (true, null);
    }

    /// <summary>Только Owner назначает/снимает Admin. Роль Owner передаётся отдельно — TransferOwnershipAsync.</summary>
    public async Task<(bool Ok, string? Error)> SetRoleAsync(Guid clubId, Guid actorId, Guid targetUserId, ClubRole role, CancellationToken ct)
    {
        if (role == ClubRole.Owner) return (false, "Владение передаётся отдельным действием.");

        var actorMembership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == actorId, ct);
        if (actorMembership is not { Status: MembershipStatus.Active, Role: ClubRole.Owner })
            return (false, "Назначать роли может только владелец клуба.");

        var target = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId && m.Status == MembershipStatus.Active, ct);
        if (target is null) return (false, "Участник не найден.");
        if (target.Role == ClubRole.Owner) return (false, "Роль владельца можно только передать целиком.");

        target.Role = role;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Два отдельных SaveChangesAsync, не один — club_members_owner_uq (partial unique
    /// WHERE role='OWNER' AND status='ACTIVE') не даст на секунду оказаться двум активным
    /// Owner одновременно, если бы EF применил апдейты в "неправильном" порядке за один раз.
    /// </summary>
    public async Task<(bool Ok, string? Error)> TransferOwnershipAsync(Guid clubId, Guid actorId, Guid targetUserId, CancellationToken ct)
    {
        var actorMembership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == actorId, ct);
        if (actorMembership is not { Status: MembershipStatus.Active, Role: ClubRole.Owner })
            return (false, "Передать владение может только текущий владелец.");
        if (targetUserId == actorId) return (false, "Вы уже владелец.");

        var target = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == targetUserId && m.Status == MembershipStatus.Active, ct);
        if (target is null) return (false, "Участник не найден.");

        actorMembership.Role = ClubRole.Admin;
        await db.SaveChangesAsync(ct);

        target.Role = ClubRole.Owner;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(List<ClubMemberDto>? Result, string? Error)> GetMembersAsync(Guid clubId, Guid viewerId, string locale, CancellationToken ct)
    {
        var viewerMembership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == viewerId, ct);
        if (viewerMembership is not { Status: MembershipStatus.Active }) return (null, "Список участников виден только участникам клуба.");

        var members = await db.ClubMembers
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Active)
            .Include(m => m.User)
            .OrderBy(m => m.Role).ThenBy(m => m.JoinedAt)
            .ToListAsync(ct);

        return (members.Select(m => new ClubMemberDto(
            m.UserId, Localized.Resolve(m.User.DisplayNameI18n, locale) ?? m.User.Handle, m.Role, m.Status, m.JoinedAt)).ToList(), null);
    }

    public async Task<(List<ClubMemberDto>? Result, string? Error)> GetJoinRequestsAsync(Guid clubId, Guid viewerId, string locale, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, viewerId, ct)) return (null, "Заявки видит только владелец или админ клуба.");

        var pending = await db.ClubMembers
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending)
            .Include(m => m.User)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

        return (pending.Select(m => new ClubMemberDto(
            m.UserId, Localized.Resolve(m.User.DisplayNameI18n, locale) ?? m.User.Handle, m.Role, m.Status, m.JoinedAt)).ToList(), null);
    }

    /// <summary>Обходит маршрутизацию по Visibility — код сам по себе уже "приглашение", единственный путь в Private без InviteAsync.</summary>
    public async Task<(Club? Result, string? Error)> JoinByInviteCodeAsync(string inviteCode, Guid userId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.InviteCode == inviteCode, ct);
        if (club is null) return (null, "Код приглашения не найден.");

        var existing = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == club.Id && m.UserId == userId, ct);
        if (existing is { Status: MembershipStatus.Active }) return (club, null);
        if (existing is { Status: MembershipStatus.Banned }) return (null, "Вы забанены в этом клубе.");

        if (existing is null)
        {
            db.ClubMembers.Add(new ClubMember { ClubId = club.Id, UserId = userId, Status = MembershipStatus.Active });
        }
        else
        {
            existing.Status = MembershipStatus.Active;
            existing.Role = ClubRole.Member;
            existing.JoinedAt = DateTime.UtcNow;
            existing.LeftAt = null;
        }

        await db.SaveChangesAsync(ct);
        await RecomputeMembersCountAsync(club.Id, ct);
        return (club, null);
    }

    public async Task<(string? Code, string? Error)> RegenerateInviteCodeAsync(Guid clubId, Guid actorId, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, actorId, ct)) return (null, "Код может обновить только владелец или админ клуба.");

        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (null, "Клуб не найден.");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
            if (await db.Clubs.AnyAsync(c => c.InviteCode == candidate, ct)) continue;

            club.InviteCode = candidate;
            await db.SaveChangesAsync(ct);
            return (candidate, null);
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный код приглашения после 5 попыток.");
    }

    /// <summary>Одна MediaAsset на клуб (AvatarId) — не галерея, тот же presign-приём, что VenueService.PresignPhotoAsync.</summary>
    public async Task<(PresignClubAvatarResponse? Result, string? Error)> PresignAvatarAsync(
        Guid clubId, Guid userId, PresignClubAvatarRequest request, CancellationToken ct)
    {
        if (!storage.IsConfigured) return (null, "Загрузка медиа пока не настроена на сервере.");
        if (!await IsOwnerOrAdminAsync(clubId, userId, ct)) return (null, "Загружать логотип может только владелец или админ клуба.");
        if (!request.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return (null, "Разрешены только изображения.");

        var extension = request.ContentType.Split('/') is [_, var ext] ? ext : "bin";
        var key = $"clubs/{clubId}/avatar-{Guid.CreateVersion7()}.{extension}";

        var media = new MediaAsset { OwnerId = userId, Kind = MediaKind.Image, BucketKey = key, MimeType = request.ContentType, SizeBytes = 0 };
        db.MediaAssets.Add(media);
        await db.SaveChangesAsync(ct);

        var uploadUrl = await storage.GetPresignedUploadUrlAsync(key, request.ContentType, TimeSpan.FromMinutes(15));
        return (new PresignClubAvatarResponse(media.Id, uploadUrl, storage.GetPublicUrl(key)), null);
    }

    public async Task<(bool Ok, string? Error)> SetAvatarAsync(Guid clubId, Guid userId, AttachClubAvatarRequest request, CancellationToken ct)
    {
        if (!await IsOwnerOrAdminAsync(clubId, userId, ct)) return (false, "Менять логотип может только владелец или админ клуба.");

        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");

        var media = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == request.MediaId && m.OwnerId == userId, ct);
        if (media is null) return (false, "Медиа не найдено.");

        club.AvatarId = media.Id;
        club.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private async Task RecomputeMembersCountAsync(Guid clubId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return;

        club.MembersCount = await db.ClubMembers.CountAsync(m => m.ClubId == clubId && m.Status == MembershipStatus.Active, ct);
        await db.SaveChangesAsync(ct);
    }

    internal async Task<bool> IsOwnerOrAdminAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var membership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        return membership is { Status: MembershipStatus.Active } && membership.Role is ClubRole.Owner or ClubRole.Admin;
    }

    /// <summary>Используется EventService — ClubId у события можно поставить только на клуб,
    /// где создатель активный участник, и Club-видимость события видна только участникам клуба.</summary>
    public Task<bool> IsActiveMemberAsync(Guid clubId, Guid userId, CancellationToken ct) =>
        db.ClubMembers.AnyAsync(m => m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Active, ct);

    private ClubDetailDto MapDetail(Club club, string locale, ClubMember? viewerMembership, int followersCount, bool viewerIsFollowing)
    {
        var viewerIsManager = viewerMembership is { Status: MembershipStatus.Active } && viewerMembership.Role is ClubRole.Owner or ClubRole.Admin;

        return new ClubDetailDto(
            club.Id, club.Slug,
            Localized.Resolve(club.NameI18n, locale) ?? "",
            Localized.Resolve(club.DescriptionI18n, locale),
            MapCity(club.City), club.Visibility, club.Kind,
            club.Avatar is null ? null : storage.GetPublicUrl(club.Avatar.BucketKey),
            club.MembersCount, club.EventsCount, club.CreatedById,
            viewerIsManager ? club.InviteCode : null,
            viewerMembership?.Role, viewerMembership?.Status,
            followersCount, viewerIsFollowing,
            club.Sports.Select(s => new SportDto(s.Sport.Id, s.Sport.Slug, s.Sport.NameI18n, s.Sport.Emoji, s.Sport.HasPositions, s.Sport.IsTeamSport)).ToList(),
            LocalizedTextDto.From(club.NameI18n),
            LocalizedTextDto.FromNullable(club.DescriptionI18n));
    }

    private static CityDto? MapCity(City? city) => city is null ? null : new CityDto(city.Id, city.Slug, city.NameI18n, city.Lat, city.Lng);

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = ClubSlugGenerator.Generate(name);
            var taken = await db.Clubs.AnyAsync(c => c.Slug == candidate, ct);
            if (!taken) return candidate;
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный слаг после 5 попыток.");
    }
}
