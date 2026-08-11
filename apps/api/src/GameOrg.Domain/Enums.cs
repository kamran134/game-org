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

public enum ClubRole { Owner, Admin, Member }

public enum MembershipStatus { Pending, Active, Banned, Left }

public enum EventType { Game, Training, Tournament, Friendly }

public enum EventVisibility { Public, Club, Unlisted }

public enum EventStatus { Draft, Scheduled, Confirmed, Cancelled, Completed }

public enum CostSplit { Free, PerPlayer, Total }

public enum GenderPolicy { Any, MenOnly, WomenOnly, MixedRequired }

public enum ParticipationStatus { Confirmed, Maybe, Waitlisted, Declined, LateCancel, NoShow, Attended }

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
}

public enum DeliveryStatus { Queued, Sent, Failed, Skipped }

public enum DevicePlatform { Ios, Android, Web }

public enum MediaKind { Image, Video }

public enum ReportReason { Spam, Abuse, FakeProfile, WrongInfo, InappropriateContent, Other }

public enum ReportStatus { Open, InReview, Resolved, Rejected }

/// <summary>API-only дискриминатор для полиморфного Follow (в таблице — три nullable FK, не эта колонка).</summary>
public enum FollowTargetType { User, Club, Venue }
