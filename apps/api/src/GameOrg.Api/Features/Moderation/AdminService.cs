using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Moderation;

/// <summary>Панель /admin (Шаг 23) — счётчики по очередям и управление пользователями
/// (поиск, роль). Сами очереди (жалобы/площадки/заявки) остаются в ReportService/
/// VenueService — здесь только то, что специфично для админки.</summary>
public sealed class AdminService(GameOrgDbContext db, AuditLogService auditLog, R2StorageService storage)
{
    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var pendingReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.InReview, ct);
        var pendingVenues = await db.Venues.CountAsync(v => v.Status == VenueStatus.Draft, ct);
        var pendingClaims = await db.VenueClaims.CountAsync(c => c.Status == ReportStatus.Open, ct);
        return new AdminOverviewDto(pendingReports, pendingVenues, pendingClaims);
    }

    /// <summary>query бьёт по users_search_trgm (Шаг 6.5) — та же генерируемая колонка,
    /// что уже используется для обычного поиска игроков.</summary>
    public async Task<List<AdminUserDto>> SearchUsersAsync(
        string? query, UserRole? role, bool? banned, string locale, int skip, int take, CancellationToken ct)
    {
        var q = db.Users.Include(u => u.Avatar).Where(u => u.DeletedAt == null).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(u => EF.Functions.ILike(u.SearchText ?? "", $"%{query}%"));
        if (role is not null)
            q = q.Where(u => u.Role == role);
        if (banned is true)
            q = q.Where(u => u.Status == UserStatus.Suspended);
        else if (banned is false)
            q = q.Where(u => u.Status != UserStatus.Suspended);

        var users = await q.OrderByDescending(u => u.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

        return users.Select(u => new AdminUserDto(
            u.Id, u.Handle, Localized.Resolve(u.DisplayNameI18n, locale) ?? u.Handle,
            u.Avatar != null ? storage.GetPublicUrl(u.Avatar.BucketKey) : null,
            u.Role, u.Status, u.CreatedAt)).ToList();
    }

    /// <summary>Нельзя менять собственную роль — иначе админ может случайно
    /// заблокировать себе доступ к панели без возможности откатить это самому.</summary>
    public async Task<(bool Ok, string? Error)> UpdateRoleAsync(Guid userId, Guid actorId, UserRole role, CancellationToken ct)
    {
        if (userId == actorId) return (false, "Нельзя изменить собственную роль.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "Пользователь не найден.");
        if (user.Role == role) return (true, null);

        var before = user.Role;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(actorId, "user.role_change", nameof(User), user.Id, new { Role = before }, new { user.Role }, ct);
        return (true, null);
    }
}
