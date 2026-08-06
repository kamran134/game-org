namespace GameOrg.Domain.Entities;

public sealed class Club
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? CityId { get; set; }
    public City? City { get; set; }
    public ClubVisibility Visibility { get; set; } = ClubVisibility.Public;
    /// <summary>chat_id Telegram-группы — связь с ботом. Null: клуб может жить без TG.</summary>
    public long? TelegramChatId { get; set; }
    public string? InviteCode { get; set; }
    public Guid? AvatarId { get; set; }
    public MediaAsset? Avatar { get; set; }
    public Guid? CoverId { get; set; }
    public MediaAsset? Cover { get; set; }

    public int MembersCount { get; set; }
    public int EventsCount { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<ClubSport> Sports { get; set; } = new List<ClubSport>();
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
}
