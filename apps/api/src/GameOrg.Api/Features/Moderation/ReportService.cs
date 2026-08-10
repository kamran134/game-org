using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Moderation;

/// <summary>Жалобы (Report) — очередь + ручное разрешение, без автоматических действий над целью
/// (см. docs/PLAN.md, Шаг 10: модератор читает жалобу и сам жмёт уже существующие
/// Опубликовать/Скрыть/Удалить/бан, если согласен). Бан живёт здесь же — иначе жалоба
/// на пользователя ничего не может сделать, эндпоинта смены статуса раньше не было вообще.</summary>
public sealed class ReportService(GameOrgDbContext db, AuditLogService auditLog)
{
    public async Task<(Report? Result, string? Error)> CreateAsync(Guid reporterId, CreateReportRequest request, CancellationToken ct)
    {
        var report = new Report
        {
            ReporterId = reporterId,
            Reason = request.Reason,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment,
        };

        switch (request.TargetType)
        {
            case ReportTargetType.Venue:
                if (!await db.Venues.AnyAsync(v => v.Id == request.TargetId, ct)) return (null, "Площадка не найдена.");
                report.TargetVenueId = request.TargetId;
                break;
            case ReportTargetType.Event:
                if (!await db.Events.AnyAsync(e => e.Id == request.TargetId, ct)) return (null, "Событие не найдено.");
                report.TargetEventId = request.TargetId;
                break;
            case ReportTargetType.User:
                if (request.TargetId == reporterId) return (null, "Нельзя пожаловаться на самого себя.");
                if (!await db.Users.AnyAsync(u => u.Id == request.TargetId, ct)) return (null, "Пользователь не найден.");
                report.TargetUserId = request.TargetId;
                break;
            case ReportTargetType.Review:
                if (!await db.VenueReviews.AnyAsync(r => r.Id == request.TargetId, ct)) return (null, "Отзыв не найден.");
                report.TargetReviewId = request.TargetId;
                break;
            default:
                return (null, "Неизвестный тип цели.");
        }

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);
        return (report, null);
    }

    /// <summary>Только модератор/админ — очередь необработанных жалоб.</summary>
    public async Task<List<ReportDto>> GetQueueAsync(string locale, CancellationToken ct)
    {
        var reports = await db.Reports
            .Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.InReview)
            .Include(r => r.Reporter)
            .Include(r => r.TargetVenue)
            .Include(r => r.TargetEvent)
            .Include(r => r.TargetUser)
            .Include(r => r.TargetReview)
            .OrderBy(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return reports.Select(r => Map(r, locale)).ToList();
    }

    public async Task<(bool Ok, string? Error)> ResolveAsync(Guid reportId, Guid resolverId, ResolveReportRequest request, CancellationToken ct)
    {
        if (request.Status != ReportStatus.Resolved && request.Status != ReportStatus.Rejected)
            return (false, "Статус может быть только Resolved или Rejected.");

        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null) return (false, "Жалоба не найдена.");

        report.Status = request.Status;
        report.ResolvedById = resolverId;
        report.ResolvedAt = DateTime.UtcNow;
        report.ResolutionNote = string.IsNullOrWhiteSpace(request.ResolutionNote) ? null : request.ResolutionNote;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(resolverId, "report.resolve", nameof(Report), report.Id, null, new { report.Status }, ct);
        return (true, null);
    }

    /// <summary>Забанить может любой Moderator/Admin, но цель-модератора/админа — только Admin (иначе один
    /// модератор мог бы забанить другого или самого себя из другого сеанса).</summary>
    public async Task<(bool Ok, string? Error)> SetUserBanAsync(Guid userId, Guid actorId, bool actorIsAdmin, bool banned, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "Пользователь не найден.");
        if (banned && user.Role != UserRole.User && !actorIsAdmin) return (false, "Забанить модератора/админа может только Admin.");

        var before = user.Status;
        user.Status = banned ? UserStatus.Suspended : UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(
            actorId, banned ? "user.ban" : "user.unban", nameof(User), user.Id,
            new { Status = before }, new { user.Status }, ct);
        return (true, null);
    }

    private static ReportDto Map(Report r, string locale)
    {
        var (targetType, targetId, summary) = r switch
        {
            { TargetVenueId: not null, TargetVenue: not null } => (ReportTargetType.Venue, r.TargetVenueId.Value, Localized.Resolve(r.TargetVenue.NameI18n, locale) ?? r.TargetVenue.Slug),
            { TargetEventId: not null, TargetEvent: not null } => (ReportTargetType.Event, r.TargetEventId.Value, Localized.Resolve(r.TargetEvent.TitleI18n, locale) ?? r.TargetEvent.PublicId),
            { TargetUserId: not null, TargetUser: not null } => (ReportTargetType.User, r.TargetUserId.Value, Localized.Resolve(r.TargetUser.DisplayNameI18n, locale) ?? r.TargetUser.Handle),
            { TargetReviewId: not null, TargetReview: not null } => (ReportTargetType.Review, r.TargetReviewId.Value, r.TargetReview.Text ?? $"★{r.TargetReview.Rating}"),
            _ => (ReportTargetType.Venue, Guid.Empty, "—"),
        };

        return new ReportDto(
            r.Id, targetType, targetId, summary, r.Reason, r.Comment, r.Status,
            r.ReporterId, Localized.Resolve(r.Reporter.DisplayNameI18n, locale) ?? r.Reporter.Handle, r.CreatedAt);
    }
}
