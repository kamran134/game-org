using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name_i18n = table.Column<string>(type: "jsonb", nullable: false),
                    desc_i18n = table.Column<string>(type: "jsonb", nullable: false),
                    icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_achievements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    name_i18n = table.Column<string>(type: "jsonb", nullable: false),
                    country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    endpoint = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_keys", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    name_i18n = table.Column<string>(type: "jsonb", nullable: false),
                    emoji = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    default_team_size = table.Column<int>(type: "integer", nullable: true),
                    has_positions = table.Column<bool>(type: "boolean", nullable: false),
                    has_score = table.Column<bool>(type: "boolean", nullable: false),
                    is_team_sport = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sport_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name_i18n = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sport_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sport_positions_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_user_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    meta = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verb = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    before = table.Column<string>(type: "jsonb", nullable: true),
                    after = table.Column<string>(type: "jsonb", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocks", x => new { x.blocker_id, x.blocked_id });
                });

            migrationBuilder.CreateTable(
                name: "club_members",
                columns: table => new
                {
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    invited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club_members", x => new { x.club_id, x.user_id });
                });

            migrationBuilder.CreateTable(
                name: "club_sports",
                columns: table => new
                {
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club_sports", x => new { x.club_id, x.sport_id });
                    table.ForeignKey(
                        name: "fk_club_sports_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clubs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: true),
                    invite_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    avatar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cover_id = table.Column<Guid>(type: "uuid", nullable: true),
                    members_count = table.Column<int>(type: "integer", nullable: false),
                    events_count = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubs", x => x.id);
                    table.ForeignKey(
                        name: "fk_clubs_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "device_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    invited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waitlist_order = table.Column<int>(type: "integer", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_participants_sport_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "sport_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_results",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    standings = table.Column<string>(type: "jsonb", nullable: true),
                    mvp_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ratings_applied = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_results", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "event_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rrule = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    duration_min = table.Column<int>(type: "integer", nullable: false),
                    template = table.Column<string>(type: "jsonb", nullable: false),
                    materialized_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_series", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_series_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_series_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    color_hex = table.Column<string>(type: "character(7)", fixedLength: true, maxLength: 7, nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    custom_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    min_participants = table.Column<int>(type: "integer", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: true),
                    waitlist_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    skill_level_min = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    skill_level_max = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    gender_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    age_min = table.Column<int>(type: "integer", nullable: true),
                    age_max = table.Column<int>(type: "integer", nullable: true),
                    cost_split = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    registration_opens_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    registration_closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_hours_before_start = table.Column<int>(type: "integer", nullable: true),
                    confirmed_count = table.Column<int>(type: "integer", nullable: false),
                    maybe_count = table.Column<int>(type: "integer", nullable: false),
                    waitlist_count = table.Column<int>(type: "integer", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_events_event_series_series_id",
                        column: x => x.series_id,
                        principalTable: "event_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_events_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_follows", x => x.id);
                    table.ForeignKey(
                        name: "fk_follows_clubs_target_club_id",
                        column: x => x.target_club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bucket_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    size_bytes = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    blurhash = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    handle = table.Column<string>(type: "citext", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    profile_visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_active_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    avatar_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_media_assets_avatar_id",
                        column: x => x.avatar_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mvp_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mvp_votes", x => x.id);
                    table.ForeignKey(
                        name: "fk_mvp_votes_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mvp_votes_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mvp_votes_users_voter_id",
                        column: x => x.voter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => new { x.user_id, x.type, x.channel });
                    table.ForeignKey(
                        name: "fk_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    dedupe_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    external_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payments_event_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "event_participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payments_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payments_users_confirmed_by_id",
                        column: x => x.confirmed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payments_users_payer_id",
                        column: x => x.payer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill = table.Column<int>(type: "integer", nullable: true),
                    fair_play = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_feedback_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_player_feedback_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_player_feedback_users_target_id",
                        column: x => x.target_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reliability_stats",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signups = table.Column<int>(type: "integer", nullable: false),
                    attended = table.Column<int>(type: "integer", nullable: false),
                    no_shows = table.Column<int>(type: "integer", nullable: false),
                    late_cancels = table.Column<int>(type: "integer", nullable: false),
                    hosted_events = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reliability_stats", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_reliability_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sport_ratings",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    deviation = table.Column<double>(type: "double precision", nullable: false),
                    volatility = table.Column<double>(type: "double precision", nullable: false),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    mvp_count = table.Column<int>(type: "integer", nullable: false),
                    peak_rating = table.Column<double>(type: "double precision", nullable: false),
                    last_played_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sport_ratings", x => new { x.user_id, x.sport_id });
                    table.ForeignKey(
                        name: "fk_sport_ratings_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sport_ratings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    context = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_achievements", x => new { x.user_id, x.achievement_id });
                    table.ForeignKey(
                        name: "fk_user_achievements_achievements_achievement_id",
                        column: x => x.achievement_id,
                        principalTable: "achievements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_achievements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    playing_since = table.Column<int>(type: "integer", nullable: true),
                    footedness = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    height_cm = table.Column<int>(type: "integer", nullable: true),
                    jersey_number = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sports", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sports_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_sports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location = table.Column<Point>(type: "geography (Point, 4326)", nullable: false),
                    is_indoor = table.Column<bool>(type: "boolean", nullable: true),
                    surface = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    has_lighting = table.Column<bool>(type: "boolean", nullable: false),
                    has_showers = table.Column<bool>(type: "boolean", nullable: false),
                    has_parking = table.Column<bool>(type: "boolean", nullable: false),
                    has_tribunes = table.Column<bool>(type: "boolean", nullable: false),
                    price_hint = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    opening_hours = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    merged_into_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating_avg = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    events_count = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                    table.ForeignKey(
                        name: "fk_venues_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_venues_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_sport_positions",
                columns: table => new
                {
                    user_sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sport_positions", x => new { x.user_sport_id, x.position_id });
                    table.ForeignKey(
                        name: "fk_user_sport_positions_sport_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "sport_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_sport_positions_user_sports_user_sport_id",
                        column: x => x.user_sport_id,
                        principalTable: "user_sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    evidence = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_venue_claims_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_venue_claims_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_venue_photos_media_assets_media_id",
                        column: x => x.media_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_venue_photos_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_venue_reviews_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_venue_reviews_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_sports",
                columns: table => new
                {
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    courts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_sports", x => new { x.venue_id, x.sport_id });
                    table.ForeignKey(
                        name: "fk_venue_sports_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_venue_sports_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_review_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_events_target_event_id",
                        column: x => x.target_event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_users_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_users_resolved_by_id",
                        column: x => x.resolved_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reports_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_venue_reviews_target_review_id",
                        column: x => x.target_review_id,
                        principalTable: "venue_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_venues_target_venue_id",
                        column: x => x.target_venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_email",
                table: "accounts",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_provider_provider_user_id",
                table: "accounts",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_user_id",
                table: "accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_achievements_code",
                table: "achievements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_activities_actor_id_created_at",
                table: "activities",
                columns: new[] { "actor_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activities_audience_created_at",
                table: "activities",
                columns: new[] { "audience", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activities_club_id_created_at",
                table: "activities",
                columns: new[] { "club_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activities_event_id",
                table: "activities",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_target_user_id",
                table: "activities",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_venue_id",
                table: "activities",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_id_created_at",
                table: "audit_logs",
                columns: new[] { "actor_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id_created_at",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_blocks_blocked_id",
                table: "blocks",
                column: "blocked_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_code_is_active",
                table: "cities",
                columns: new[] { "country_code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_slug",
                table: "cities",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_club_members_club_id_role",
                table: "club_members",
                columns: new[] { "club_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_club_members_invited_by_id",
                table: "club_members",
                column: "invited_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_club_members_user_id_status",
                table: "club_members",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_club_sports_sport_id",
                table: "club_sports",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "ix_clubs_avatar_id",
                table: "clubs",
                column: "avatar_id");

            migrationBuilder.CreateIndex(
                name: "ix_clubs_city_id_visibility",
                table: "clubs",
                columns: new[] { "city_id", "visibility" });

            migrationBuilder.CreateIndex(
                name: "ix_clubs_cover_id",
                table: "clubs",
                column: "cover_id");

            migrationBuilder.CreateIndex(
                name: "ix_clubs_created_by_id",
                table: "clubs",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_clubs_invite_code",
                table: "clubs",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clubs_telegram_chat_id",
                table: "clubs",
                column: "telegram_chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_tokens_token",
                table: "device_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_tokens_user_id",
                table: "device_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_event_id_status",
                table: "event_participants",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_event_id_user_id",
                table: "event_participants",
                columns: new[] { "event_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_event_id_waitlist_order",
                table: "event_participants",
                columns: new[] { "event_id", "waitlist_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_invited_by_id",
                table: "event_participants",
                column: "invited_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_position_id",
                table: "event_participants",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_team_id",
                table: "event_participants",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_user_id_status",
                table: "event_participants",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_results_recorded_by_id",
                table: "event_results",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_club_id",
                table: "event_series",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_created_by_id",
                table: "event_series",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_is_active_materialized_until",
                table: "event_series",
                columns: new[] { "is_active", "materialized_until" });

            migrationBuilder.CreateIndex(
                name: "ix_event_series_sport_id",
                table: "event_series",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_venue_id",
                table: "event_series",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_event_id",
                table: "event_teams",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_club_id_starts_at",
                table: "events",
                columns: new[] { "club_id", "starts_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by_id",
                table: "events",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_public_id",
                table: "events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_series_id",
                table: "events",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_sport_id_starts_at",
                table: "events",
                columns: new[] { "sport_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_events_status_starts_at",
                table: "events",
                columns: new[] { "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_events_venue_id_starts_at",
                table: "events",
                columns: new[] { "venue_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_events_visibility_status_starts_at",
                table: "events",
                columns: new[] { "visibility", "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_follows_follower_id_target_club_id",
                table: "follows",
                columns: new[] { "follower_id", "target_club_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_follows_follower_id_target_user_id",
                table: "follows",
                columns: new[] { "follower_id", "target_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_follows_follower_id_target_venue_id",
                table: "follows",
                columns: new[] { "follower_id", "target_venue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_follows_target_club_id",
                table: "follows",
                column: "target_club_id");

            migrationBuilder.CreateIndex(
                name: "ix_follows_target_user_id",
                table: "follows",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_follows_target_venue_id",
                table: "follows",
                column: "target_venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_bucket_key",
                table: "media_assets",
                column: "bucket_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_owner_id",
                table: "media_assets",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_mvp_votes_event_id_target_user_id",
                table: "mvp_votes",
                columns: new[] { "event_id", "target_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mvp_votes_event_id_voter_id",
                table: "mvp_votes",
                columns: new[] { "event_id", "voter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mvp_votes_target_user_id",
                table: "mvp_votes",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mvp_votes_voter_id",
                table: "mvp_votes",
                column: "voter_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_dedupe_key_channel",
                table: "notifications",
                columns: new[] { "dedupe_key", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_status_scheduled_for",
                table: "notifications",
                columns: new[] { "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read_at",
                table: "notifications",
                columns: new[] { "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_processed_at_created_at",
                table: "outbox_events",
                columns: new[] { "processed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_club_id_created_at",
                table: "payments",
                columns: new[] { "club_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_payments_confirmed_by_id",
                table: "payments",
                column: "confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_event_id_status",
                table: "payments",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_participant_id",
                table: "payments",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payer_id_status",
                table: "payments",
                columns: new[] { "payer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_provider_external_id",
                table: "payments",
                columns: new[] { "provider", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_feedback_author_id",
                table: "player_feedback",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_feedback_event_id_author_id_target_id",
                table: "player_feedback",
                columns: new[] { "event_id", "author_id", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_feedback_target_id",
                table: "player_feedback",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "ix_reliability_stats_score",
                table: "reliability_stats",
                column: "score");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_resolved_by_id",
                table: "reports",
                column: "resolved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status_created_at",
                table: "reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_event_id",
                table: "reports",
                column: "target_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_review_id",
                table: "reports",
                column: "target_review_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_user_id",
                table: "reports",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_venue_id",
                table: "reports",
                column: "target_venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_expires_at",
                table: "sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_refresh_token_hash",
                table: "sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id_revoked_at",
                table: "sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sport_positions_sport_id_code",
                table: "sport_positions",
                columns: new[] { "sport_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sport_ratings_sport_id_rating",
                table: "sport_ratings",
                columns: new[] { "sport_id", "rating" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_sports_slug",
                table: "sports",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_achievements_achievement_id",
                table: "user_achievements",
                column: "achievement_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_achievements_user_id_earned_at",
                table: "user_achievements",
                columns: new[] { "user_id", "earned_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_user_sport_positions_position_id",
                table: "user_sport_positions",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sports_sport_id_level",
                table: "user_sports",
                columns: new[] { "sport_id", "level" });

            migrationBuilder.CreateIndex(
                name: "ix_user_sports_user_id_sport_id",
                table: "user_sports",
                columns: new[] { "user_id", "sport_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_avatar_id",
                table: "users",
                column: "avatar_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_city_id_status",
                table: "users",
                columns: new[] { "city_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_users_last_active_at",
                table: "users",
                column: "last_active_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_venue_claims_user_id",
                table: "venue_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_claims_venue_id_user_id",
                table: "venue_claims",
                columns: new[] { "venue_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_photos_media_id",
                table: "venue_photos",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_photos_venue_id_media_id",
                table: "venue_photos",
                columns: new[] { "venue_id", "media_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_reviews_author_id",
                table: "venue_reviews",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_reviews_venue_id_author_id",
                table: "venue_reviews",
                columns: new[] { "venue_id", "author_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_reviews_venue_id_created_at",
                table: "venue_reviews",
                columns: new[] { "venue_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_venue_sports_sport_id",
                table: "venue_sports",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "ix_venues_city_id_status",
                table: "venues",
                columns: new[] { "city_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_venues_created_by_id",
                table: "venues",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_venues_location",
                table: "venues",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "ix_venues_status_rating_avg",
                table: "venues",
                columns: new[] { "status", "rating_avg" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "fk_accounts_users_user_id",
                table: "accounts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_clubs_club_id",
                table: "activities",
                column: "club_id",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_events_event_id",
                table: "activities",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_users_actor_id",
                table: "activities",
                column: "actor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_users_target_user_id",
                table: "activities",
                column: "target_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_venues_venue_id",
                table: "activities",
                column: "venue_id",
                principalTable: "venues",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_users_actor_id",
                table: "audit_logs",
                column: "actor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_users_blocked_id",
                table: "blocks",
                column: "blocked_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_users_blocker_id",
                table: "blocks",
                column: "blocker_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_club_members_clubs_club_id",
                table: "club_members",
                column: "club_id",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_club_members_users_invited_by_id",
                table: "club_members",
                column: "invited_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_club_members_users_user_id",
                table: "club_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_club_sports_clubs_club_id",
                table: "club_sports",
                column: "club_id",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_clubs_media_assets_avatar_id",
                table: "clubs",
                column: "avatar_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_clubs_media_assets_cover_id",
                table: "clubs",
                column: "cover_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_clubs_users_created_by_id",
                table: "clubs",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_device_tokens_users_user_id",
                table: "device_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_participants_event_teams_team_id",
                table: "event_participants",
                column: "team_id",
                principalTable: "event_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_participants_events_event_id",
                table: "event_participants",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_participants_users_invited_by_id",
                table: "event_participants",
                column: "invited_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_participants_users_user_id",
                table: "event_participants",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_results_events_event_id",
                table: "event_results",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_results_users_recorded_by_id",
                table: "event_results",
                column: "recorded_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_series_users_created_by_id",
                table: "event_series",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_series_venues_venue_id",
                table: "event_series",
                column: "venue_id",
                principalTable: "venues",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_teams_events_event_id",
                table: "event_teams",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_users_created_by_id",
                table: "events",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_events_venues_venue_id",
                table: "events",
                column: "venue_id",
                principalTable: "venues",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_follows_users_follower_id",
                table: "follows",
                column: "follower_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_follows_users_target_user_id",
                table: "follows",
                column: "target_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_follows_venues_target_venue_id",
                table: "follows",
                column: "target_venue_id",
                principalTable: "venues",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_media_assets_users_owner_id",
                table: "media_assets",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_media_assets_users_owner_id",
                table: "media_assets");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "blocks");

            migrationBuilder.DropTable(
                name: "club_members");

            migrationBuilder.DropTable(
                name: "club_sports");

            migrationBuilder.DropTable(
                name: "device_tokens");

            migrationBuilder.DropTable(
                name: "event_results");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "idempotency_keys");

            migrationBuilder.DropTable(
                name: "mvp_votes");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "outbox_events");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "player_feedback");

            migrationBuilder.DropTable(
                name: "reliability_stats");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "sport_ratings");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "user_sport_positions");

            migrationBuilder.DropTable(
                name: "venue_claims");

            migrationBuilder.DropTable(
                name: "venue_photos");

            migrationBuilder.DropTable(
                name: "venue_sports");

            migrationBuilder.DropTable(
                name: "event_participants");

            migrationBuilder.DropTable(
                name: "venue_reviews");

            migrationBuilder.DropTable(
                name: "achievements");

            migrationBuilder.DropTable(
                name: "user_sports");

            migrationBuilder.DropTable(
                name: "event_teams");

            migrationBuilder.DropTable(
                name: "sport_positions");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "event_series");

            migrationBuilder.DropTable(
                name: "clubs");

            migrationBuilder.DropTable(
                name: "sports");

            migrationBuilder.DropTable(
                name: "venues");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
