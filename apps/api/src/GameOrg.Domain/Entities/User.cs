namespace GameOrg.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Публичный ник: game.org.az/@rustam.</summary>
    public required string Handle { get; set; }
    public required string DisplayName { get; set; }
    public string? Bio { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Phone { get; set; }
    public string Locale { get; set; } = "ru";
    public string Timezone { get; set; } = "Asia/Baku";
    public UserStatus Status { get; set; } = UserStatus.Active;
    /// <summary>«Галочка»: тренер, владелец площадки.</summary>
    public bool IsVerified { get; set; }
    public Visibility ProfileVisibility { get; set; } = Visibility.Public;
    public DateTime? LastActiveAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }
    public Guid? AvatarId { get; set; }
    public MediaAsset? Avatar { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<UserSport> Sports { get; set; } = new List<UserSport>();
    public ICollection<ClubMember> ClubMemberships { get; set; } = new List<ClubMember>();
    public ICollection<EventParticipant> Participations { get; set; } = new List<EventParticipant>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SportRating> Ratings { get; set; } = new List<SportRating>();
    public ICollection<DeviceToken> Devices { get; set; } = new List<DeviceToken>();
    public ReliabilityStat? Reliability { get; set; }
}
