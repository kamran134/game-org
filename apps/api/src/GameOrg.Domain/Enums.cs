namespace GameOrg.Domain;

public enum UserStatus { Active, Suspended, Deactivated }

/// <summary>Платформенная роль — не путать с ClubRole (роль внутри клуба). Moderator ⊂ Admin.</summary>
public enum UserRole { User, Moderator, Admin }

public enum Gender { Male, Female, Other }

public enum AuthProvider { Telegram, Email, Google, Apple }

public enum Visibility { Public, Followers, Private }

public enum SkillLevel { Beginner, Amateur, Intermediate, Advanced, SemiPro, Pro }

public enum Footedness { Left, Right, Both }

public enum VenueSurface { NaturalGrass, ArtificialGrass, Parquet, Rubber, Sand, Concrete, Ice, Water, Other }

public enum VenueStatus { Draft, Published, Hidden, Merged }

public enum ClubVisibility { Public, RequestOnly, Private }

/// <summary>Шаг 20 — группы не отдельная сущность, а вид клуба: та же роль/членство/заявки/инвайты.</summary>
public enum ClubKind { Club, Group }

public enum ClubRole { Owner, Admin, Member }

public enum MembershipStatus { Pending, Active, Banned, Left }

public enum EventType { Game, Training, Tournament, Friendly }

public enum EventVisibility { Public, Club, Unlisted }

public enum EventStatus { Draft, Scheduled, Confirmed, Cancelled, Completed }

public enum CostSplit { Free, PerPlayer, Total }

public enum GenderPolicy { Any, MenOnly, WomenOnly, MixedRequired }

public enum ParticipationStatus { Confirmed, Maybe, Waitlisted, Declined, LateCancel, NoShow, Attended, PendingApproval }

public enum PaymentStatus { Pending, Paid, Failed, Refunded, Cancelled }

public enum PaymentMethod { Cash, BankTransfer, CardOnline, Balance }

public enum ActivityVerb
{
    CreatedEvent, JoinedEvent, CompletedEvent, JoinedClub, CreatedClub,
    ReviewedVenue, EarnedAchievement, AddedSport, RatingMilestone,
}

public enum NotificationChannel { Telegram, Push, Email, InApp }

public enum NotificationType
{
    EventReminder24h, EventReminder2h, EventCreated, EventUpdated, EventCancelled, EventConfirmed,
    ParticipantJoined, ParticipantLeft, WaitlistPromoted, PaymentDue, PaymentConfirmed,
    ClubInvite, ClubJoinRequest, NewFollower, MvpVoteOpen, ResultPosted,
    // Не было зарезервировано в Шаге 2 — добавлено в Шаге 16. Хранится строкой
    // в БД (HasConversion<string>()), добавление значения не ломает существующие данные.
    AchievementEarned,
    // Шаг 19 — заявки на события с RequiresApproval.
    EventJoinRequest, EventJoinApproved, EventJoinRejected,
}

public enum DeliveryStatus { Queued, Sent, Failed, Skipped }

public enum DevicePlatform { Ios, Android, Web }

public enum MediaKind { Image, Video }

public enum ReportReason { Spam, Abuse, FakeProfile, WrongInfo, InappropriateContent, Other }

public enum ReportStatus { Open, InReview, Resolved, Rejected }

/// <summary>API-only дискриминатор для полиморфного Follow (в таблице — три nullable FK, не эта колонка).</summary>
public enum FollowTargetType { User, Club, Venue }
