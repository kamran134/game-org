using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrg.Infrastructure.Migrations
{
    /// <summary>
    /// То, что EF Core / fluent-конфигурация не выражает: CHECK-констрейнты,
    /// partial unique-индексы, поиск по триграммам. Источник — docs/schema/001_constraints.sql.
    /// Пропущены: блок 1 (geom-триггер — не нужен, Location уже типизирован через
    /// NetTopologySuite.Point с GIST-индексом в InitialSchema) и блок 12
    /// (партиционирование — план на будущее, не применяется сейчас).
    /// </summary>
    public partial class Constraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- 2. Partial unique: handle/slug уникальны только среди неудалённых.
                CREATE UNIQUE INDEX users_handle_active_uq  ON users (handle)  WHERE deleted_at IS NULL;
                CREATE UNIQUE INDEX venues_slug_active_uq   ON venues (slug)   WHERE deleted_at IS NULL;
                CREATE UNIQUE INDEX clubs_slug_active_uq    ON clubs (slug)    WHERE deleted_at IS NULL;

                ALTER TABLE users ADD CONSTRAINT users_handle_format
                  CHECK (handle ~ '^[a-z][a-z0-9_]{2,29}$');


                -- 3. Follow: ровно одна цель + нельзя подписаться на себя.
                ALTER TABLE follows ADD CONSTRAINT follows_exactly_one_target CHECK (
                  (target_user_id IS NOT NULL)::int
                + (target_club_id IS NOT NULL)::int
                + (target_venue_id IS NOT NULL)::int = 1
                );
                ALTER TABLE follows ADD CONSTRAINT follows_no_self
                  CHECK (target_user_id IS NULL OR target_user_id <> follower_id);

                ALTER TABLE blocks ADD CONSTRAINT blocks_no_self CHECK (blocker_id <> blocked_id);


                -- 4. Report: ровно одна цель.
                ALTER TABLE reports ADD CONSTRAINT reports_exactly_one_target CHECK (
                  (target_user_id IS NOT NULL)::int
                + (target_event_id IS NOT NULL)::int
                + (target_venue_id IS NOT NULL)::int
                + (target_review_id IS NOT NULL)::int = 1
                );


                -- 5. Участник: либо зарегистрированный пользователь, либо гость с именем.
                ALTER TABLE event_participants ADD CONSTRAINT participants_user_xor_guest CHECK (
                  (user_id IS NOT NULL AND guest_name IS NULL)
                  OR (user_id IS NULL AND guest_name IS NOT NULL AND invited_by_id IS NOT NULL)
                );

                ALTER TABLE event_participants ADD CONSTRAINT participants_waitlist_order CHECK (
                  (status = 'Waitlisted') = (waitlist_order IS NOT NULL)
                );

                CREATE UNIQUE INDEX participants_waitlist_uq
                  ON event_participants (event_id, waitlist_order)
                  WHERE waitlist_order IS NOT NULL;


                -- 6. Событие: инварианты времени, вместимости и места.
                ALTER TABLE events ADD CONSTRAINT events_time_order CHECK (ends_at > starts_at);
                ALTER TABLE events ADD CONSTRAINT events_capacity CHECK (
                  max_participants IS NULL OR min_participants IS NULL
                  OR max_participants >= min_participants
                );
                ALTER TABLE events ADD CONSTRAINT events_has_place CHECK (
                  venue_id IS NOT NULL OR custom_location IS NOT NULL
                );
                ALTER TABLE events ADD CONSTRAINT events_cost_positive CHECK (cost IS NULL OR cost >= 0);
                ALTER TABLE events ADD CONSTRAINT events_cost_required CHECK (
                  cost_split = 'Free' OR cost IS NOT NULL
                );
                ALTER TABLE events ADD CONSTRAINT events_club_visibility CHECK (
                  visibility <> 'Club' OR club_id IS NOT NULL
                );


                -- 7. Оценки 1..5.
                ALTER TABLE venue_reviews    ADD CONSTRAINT venue_reviews_rating_range CHECK (rating BETWEEN 1 AND 5);
                ALTER TABLE player_feedback  ADD CONSTRAINT feedback_skill_range     CHECK (skill IS NULL OR skill BETWEEN 1 AND 5);
                ALTER TABLE player_feedback  ADD CONSTRAINT feedback_fairplay_range  CHECK (fair_play IS NULL OR fair_play BETWEEN 1 AND 5);
                ALTER TABLE player_feedback  ADD CONSTRAINT feedback_no_self         CHECK (author_id <> target_id);
                ALTER TABLE mvp_votes        ADD CONSTRAINT mvp_no_self              CHECK (voter_id <> target_user_id);

                ALTER TABLE reliability_stats ADD CONSTRAINT reliability_score_range CHECK (score BETWEEN 0 AND 100);


                -- 8. Ровно один primary-спорт у игрока и ровно одна primary-позиция.
                CREATE UNIQUE INDEX user_sports_primary_uq
                  ON user_sports (user_id) WHERE is_primary;
                CREATE UNIQUE INDEX user_sport_positions_primary_uq
                  ON user_sport_positions (user_sport_id) WHERE is_primary;
                CREATE UNIQUE INDEX venue_photos_cover_uq
                  ON venue_photos (venue_id) WHERE is_cover;


                -- 9. Клуб: ровно один OWNER среди активных.
                CREATE UNIQUE INDEX club_members_owner_uq
                  ON club_members (club_id) WHERE role = 'Owner' AND status = 'Active';


                -- 10. Полнотекстовый поиск: игроки, площадки, клубы.
                CREATE INDEX users_search_trgm  ON users  USING GIN (display_name gin_trgm_ops, handle gin_trgm_ops);
                CREATE INDEX venues_search_trgm ON venues USING GIN (name gin_trgm_ops, address gin_trgm_ops);
                CREATE INDEX clubs_search_trgm  ON clubs  USING GIN (name gin_trgm_ops);


                -- 11. Ключевой индекс дискавери: только будущие публичные события.
                CREATE INDEX events_upcoming_public
                  ON events (sport_id, starts_at)
                  WHERE visibility = 'Public'
                    AND status IN ('Scheduled', 'Confirmed')
                    AND deleted_at IS NULL;

                CREATE INDEX outbox_unprocessed
                  ON outbox_events (created_at)
                  WHERE processed_at IS NULL;

                CREATE INDEX notifications_pending
                  ON notifications (scheduled_for)
                  WHERE status = 'Queued';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS notifications_pending;
                DROP INDEX IF EXISTS outbox_unprocessed;
                DROP INDEX IF EXISTS events_upcoming_public;

                DROP INDEX IF EXISTS clubs_search_trgm;
                DROP INDEX IF EXISTS venues_search_trgm;
                DROP INDEX IF EXISTS users_search_trgm;

                DROP INDEX IF EXISTS club_members_owner_uq;

                DROP INDEX IF EXISTS venue_photos_cover_uq;
                DROP INDEX IF EXISTS user_sport_positions_primary_uq;
                DROP INDEX IF EXISTS user_sports_primary_uq;

                ALTER TABLE reliability_stats DROP CONSTRAINT IF EXISTS reliability_score_range;

                ALTER TABLE mvp_votes       DROP CONSTRAINT IF EXISTS mvp_no_self;
                ALTER TABLE player_feedback DROP CONSTRAINT IF EXISTS feedback_no_self;
                ALTER TABLE player_feedback DROP CONSTRAINT IF EXISTS feedback_fairplay_range;
                ALTER TABLE player_feedback DROP CONSTRAINT IF EXISTS feedback_skill_range;
                ALTER TABLE venue_reviews   DROP CONSTRAINT IF EXISTS venue_reviews_rating_range;

                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_club_visibility;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_cost_required;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_cost_positive;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_has_place;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_capacity;
                ALTER TABLE events DROP CONSTRAINT IF EXISTS events_time_order;

                DROP INDEX IF EXISTS participants_waitlist_uq;
                ALTER TABLE event_participants DROP CONSTRAINT IF EXISTS participants_waitlist_order;
                ALTER TABLE event_participants DROP CONSTRAINT IF EXISTS participants_user_xor_guest;

                ALTER TABLE reports DROP CONSTRAINT IF EXISTS reports_exactly_one_target;

                ALTER TABLE blocks  DROP CONSTRAINT IF EXISTS blocks_no_self;
                ALTER TABLE follows DROP CONSTRAINT IF EXISTS follows_no_self;
                ALTER TABLE follows DROP CONSTRAINT IF EXISTS follows_exactly_one_target;

                ALTER TABLE users DROP CONSTRAINT IF EXISTS users_handle_format;
                DROP INDEX IF EXISTS clubs_slug_active_uq;
                DROP INDEX IF EXISTS venues_slug_active_uq;
                DROP INDEX IF EXISTS users_handle_active_uq;
                """);
        }
    }
}
