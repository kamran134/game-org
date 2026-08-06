using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Infrastructure;

public sealed class GameOrgDbContext(DbContextOptions<GameOrgDbContext> options) : DbContext(options)
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<SportPosition> SportPositions => Set<SportPosition>();
    public DbSet<UserSport> UserSports => Set<UserSport>();
    public DbSet<UserSportPosition> UserSportPositions => Set<UserSportPosition>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<VenueSport> VenueSports => Set<VenueSport>();
    public DbSet<VenuePhoto> VenuePhotos => Set<VenuePhoto>();
    public DbSet<VenueReview> VenueReviews => Set<VenueReview>();
    public DbSet<VenueClaim> VenueClaims => Set<VenueClaim>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubSport> ClubSports => Set<ClubSport>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<EventSeries> EventSeries => Set<EventSeries>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<EventTeam> EventTeams => Set<EventTeam>();
    public DbSet<EventResult> EventResults => Set<EventResult>();
    public DbSet<MvpVote> MvpVotes => Set<MvpVote>();
    public DbSet<SportRating> SportRatings => Set<SportRating>();
    public DbSet<ReliabilityStat> ReliabilityStats => Set<ReliabilityStat>();
    public DbSet<PlayerFeedback> PlayerFeedback => Set<PlayerFeedback>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("btree_gin");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameOrgDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Все DateTime — UTC. Postgres/Npgsql иначе кидает исключение на Kind=Unspecified.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        // Деньги по умолчанию (10,2); у полей вроде Venue.RatingAvg своя точность —
        // переопределяется явно в конфигурации сущности.
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }
}
