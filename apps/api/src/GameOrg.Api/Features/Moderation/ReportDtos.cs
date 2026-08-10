using GameOrg.Domain;

namespace GameOrg.Api.Features.Moderation;

/// <summary>Клиентское удобство поверх денормализованных FK в Report (TargetUserId/TargetEventId/TargetVenueId/TargetReviewId).</summary>
public enum ReportTargetType { Venue, Event, User, Review }

public sealed record CreateReportRequest(ReportTargetType TargetType, Guid TargetId, ReportReason Reason, string? Comment);

public sealed record ReportDto(
    Guid Id,
    ReportTargetType TargetType,
    Guid TargetId,
    string TargetSummary,
    ReportReason Reason,
    string? Comment,
    ReportStatus Status,
    Guid ReporterId,
    string ReporterDisplayName,
    DateTime CreatedAt);

public sealed record ResolveReportRequest(ReportStatus Status, string? ResolutionNote);
