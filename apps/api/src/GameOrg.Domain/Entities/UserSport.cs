namespace GameOrg.Domain.Entities;

/// <summary>
/// Суб-профиль игрока по конкретному виду спорта.
/// Один человек = несколько UserSport, у каждого своя история и рейтинг.
/// </summary>
public sealed class UserSport
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public SkillLevel Level { get; set; } = SkillLevel.Amateur;
    public bool IsPrimary { get; set; }
    public int? PlayingSince { get; set; }
    public Footedness? Footedness { get; set; }
    public int? HeightCm { get; set; }
    public int? JerseyNumber { get; set; }
    public string? Note { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Public;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserSportPosition> Positions { get; set; } = new List<UserSportPosition>();
}
