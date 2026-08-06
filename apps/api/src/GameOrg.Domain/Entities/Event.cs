namespace GameOrg.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Короткий публичный id для ссылок game.org.az/e/{PublicId}.</summary>
    public required string PublicId { get; set; }

    public EventType Type { get; set; } = EventType.Game;
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }
    /// <summary>Если площадки нет в каталоге — свободный текст. Хотя бы одно из двух.</summary>
    public string? CustomLocation { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Timezone { get; set; } = "Asia/Baku";

    public int? MinParticipants { get; set; }
    public int? MaxParticipants { get; set; }
    public bool WaitlistEnabled { get; set; } = true;

    public SkillLevel? SkillLevelMin { get; set; }
    public SkillLevel? SkillLevelMax { get; set; }
    public GenderPolicy GenderPolicy { get; set; } = GenderPolicy.Any;
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }

    public CostSplit CostSplit { get; set; } = CostSplit.Free;
    public decimal? Cost { get; set; }
    public string Currency { get; set; } = "AZN";

    public DateTime? RegistrationOpensAt { get; set; }
    /// <summary>
    /// Абсолютный дедлайн записи. Вычисляется из LockHoursBeforeStart при
    /// создании/переносе — чтобы не считать его в каждом запросе.
    /// </summary>
    public DateTime? RegistrationClosesAt { get; set; }
    public int? LockHoursBeforeStart { get; set; }

    public int ConfirmedCount { get; set; }
    public int MaybeCount { get; set; }
    public int WaitlistCount { get; set; }

    public Guid? SeriesId { get; set; }
    public EventSeries? Series { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    public ICollection<EventTeam> Teams { get; set; } = new List<EventTeam>();
    public EventResult? Result { get; set; }
}
