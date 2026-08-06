namespace GameOrg.Domain.Entities;

/// <summary>
/// Подписка на игрока / клуб / площадку.
/// Вместо полиморфизма — три nullable FK + CHECK «ровно один заполнен» (в БД),
/// чтобы не терять ссылочную целостность.
/// </summary>
public sealed class Follow
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
    public Guid? TargetClubId { get; set; }
    public Club? TargetClub { get; set; }
    public Guid? TargetVenueId { get; set; }
    public Venue? TargetVenue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
