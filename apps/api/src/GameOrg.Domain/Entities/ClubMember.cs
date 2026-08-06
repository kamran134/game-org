namespace GameOrg.Domain.Entities;

public sealed class ClubMember
{
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ClubRole Role { get; set; } = ClubRole.Member;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public Guid? InvitedById { get; set; }
    public User? InvitedBy { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}
