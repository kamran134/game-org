namespace GameOrg.Domain.Entities;

public sealed class Report
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    public ReportReason Reason { get; set; }
    public string? Comment { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
    public Guid? TargetEventId { get; set; }
    public Event? TargetEvent { get; set; }
    public Guid? TargetVenueId { get; set; }
    public Venue? TargetVenue { get; set; }
    public Guid? TargetReviewId { get; set; }
    public VenueReview? TargetReview { get; set; }

    public Guid? ResolvedById { get; set; }
    public User? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
