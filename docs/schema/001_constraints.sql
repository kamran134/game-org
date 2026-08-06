-- =============================================================================
--  Дополнение к schema.prisma: то, что Prisma не умеет выразить.
--  Кладётся в prisma/migrations/<ts>_constraints/migration.sql
--  (или создаётся пустая миграция через `prisma migrate dev --create-only`).
-- =============================================================================

-- 1. PostGIS: geom как generated-колонка из lat/lng + GIST-индекс.
--    Радиусный поиск: ST_DWithin(geom, ST_MakePoint(:lng,:lat)::geography, :meters)
ALTER TABLE venues
  ALTER COLUMN geom TYPE geography(Point, 4326)
  USING ST_SetSRID(ST_MakePoint(lng, lat), 4326)::geography;

ALTER TABLE venues
  ADD CONSTRAINT venues_geom_matches_latlng
  CHECK (geom IS NULL OR ST_X(geom::geometry) = lng);

CREATE INDEX venues_geom_gist ON venues USING GIST (geom);

CREATE OR REPLACE FUNCTION venues_sync_geom() RETURNS trigger AS $$
BEGIN
  NEW.geom := ST_SetSRID(ST_MakePoint(NEW.lng, NEW.lat), 4326)::geography;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER venues_sync_geom_trg
  BEFORE INSERT OR UPDATE OF lat, lng ON venues
  FOR EACH ROW EXECUTE FUNCTION venues_sync_geom();


-- 2. Partial unique: handle/slug уникальны только среди неудалённых.
--    (обычный @unique блокировал бы переиспользование ника после удаления)
CREATE UNIQUE INDEX users_handle_active_uq  ON users (handle)  WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX venues_slug_active_uq   ON venues (slug)   WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX clubs_slug_active_uq    ON clubs (slug)    WHERE deleted_at IS NULL;

ALTER TABLE users  DROP CONSTRAINT IF EXISTS users_handle_key;
ALTER TABLE venues DROP CONSTRAINT IF EXISTS venues_slug_key;
ALTER TABLE clubs  DROP CONSTRAINT IF EXISTS clubs_slug_key;

-- handle: только [a-z0-9_], 3..30, не начинается с цифры
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

-- waitlist_order заполнен тогда и только тогда, когда статус WAITLISTED
ALTER TABLE event_participants ADD CONSTRAINT participants_waitlist_order CHECK (
  (status = 'WAITLISTED') = (waitlist_order IS NOT NULL)
);

-- очередь без дырок и коллизий
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
  cost_split = 'FREE' OR cost IS NOT NULL
);
-- CLUB-видимость обязана иметь клуб
ALTER TABLE events ADD CONSTRAINT events_club_visibility CHECK (
  visibility <> 'CLUB' OR club_id IS NOT NULL
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
  ON club_members (club_id) WHERE role = 'OWNER' AND status = 'ACTIVE';


-- 10. Полнотекстовый поиск: игроки, площадки, клубы.
CREATE INDEX users_search_trgm  ON users  USING GIN (display_name gin_trgm_ops, handle gin_trgm_ops);
CREATE INDEX venues_search_trgm ON venues USING GIN (name gin_trgm_ops, address gin_trgm_ops);
CREATE INDEX clubs_search_trgm  ON clubs  USING GIN (name gin_trgm_ops);


-- 11. Ключевой индекс дискавери: только будущие публичные события.
--     Держит индекс маленьким при любом объёме архива.
CREATE INDEX events_upcoming_public
  ON events (sport_id, starts_at)
  WHERE visibility = 'PUBLIC'
    AND status IN ('SCHEDULED', 'CONFIRMED')
    AND deleted_at IS NULL;

-- Outbox-релей: индекс только по необработанным.
CREATE INDEX outbox_unprocessed
  ON outbox_events (created_at)
  WHERE processed_at IS NULL;

-- Очередь отправки уведомлений.
CREATE INDEX notifications_pending
  ON notifications (scheduled_for)
  WHERE status = 'QUEUED';


-- 12. Партиционирование архива (когда events перевалит за ~5-10 млн).
--     Пока НЕ применять — оставлено как план:
--     events → RANGE по starts_at (по годам),
--     activities и audit_logs → RANGE по created_at (по месяцам),
--     notifications → RANGE по created_at + DROP старых партиций вместо DELETE.
