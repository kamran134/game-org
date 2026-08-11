"use client";

import { useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getFeed, type Activity } from "@/lib/socialApi";

const PAGE_SIZE = 20;

function ActivityLine({ activity }: { activity: Activity }) {
  const t = useTranslations("Social");
  const actorLink = (
    <Link href={`/${activity.actor.handle}`} className="font-medium text-foreground hover:underline">
      {activity.actor.name}
    </Link>
  );

  switch (activity.verb) {
    case "CreatedEvent":
    case "JoinedEvent":
      return (
        <>
          {actorLink} {t(activity.verb === "CreatedEvent" ? "activity.createdEvent" : "activity.joinedEvent")}{" "}
          {activity.event && (
            <Link href={`/events/${activity.event.publicId}`} className="font-medium text-brand-primary hover:underline">
              {activity.event.title ?? t("activity.untitledEvent")}
            </Link>
          )}
        </>
      );
    case "CreatedClub":
    case "JoinedClub":
      return (
        <>
          {actorLink} {t(activity.verb === "CreatedClub" ? "activity.createdClub" : "activity.joinedClub")}{" "}
          {activity.club && (
            <Link href={`/clubs/${activity.club.slug}`} className="font-medium text-brand-primary hover:underline">
              {activity.club.name}
            </Link>
          )}
        </>
      );
    case "ReviewedVenue":
      return (
        <>
          {actorLink} {t("activity.reviewedVenue")}{" "}
          {activity.venue && (
            <Link href={`/venues/${activity.venue.slug}`} className="font-medium text-brand-primary hover:underline">
              {activity.venue.name}
            </Link>
          )}
        </>
      );
    case "AddedSport":
      return (
        <>
          {actorLink} {t("activity.addedSport")}
        </>
      );
    default:
      return (
        <>
          {actorLink} {t("activity.other")}
        </>
      );
  }
}

export function ActivityFeed({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Social");
  const locale = useLocale();
  const format = useFormatter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [items, setItems] = useState<Activity[] | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  useEffect(() => {
    if (!me) return;
    getFeed(apiUrl, locale, { take: PAGE_SIZE })
      .then((result) => {
        setItems(result);
        setHasMore(result.length === PAGE_SIZE);
      })
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, locale, me, t]);

  async function handleLoadMore() {
    if (!items) return;
    setLoadingMore(true);
    try {
      const more = await getFeed(apiUrl, locale, { skip: items.length, take: PAGE_SIZE });
      setItems((prev) => [...(prev ?? []), ...more]);
      setHasMore(more.length === PAGE_SIZE);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("loadError"));
    } finally {
      setLoadingMore(false);
    }
  }

  if (me === undefined) return null;

  if (!me) {
    return (
      <div className="flex flex-col gap-4">
        <h1 className="font-heading text-2xl font-semibold text-foreground">{t("feedTitle")}</h1>
        <p className="text-foreground/70">
          {t("loginRequired")}{" "}
          <Link href="/login" className="font-medium text-brand-primary hover:underline">
            {t("loginLink")}
          </Link>
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("feedTitle")}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {items === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-brand-border px-5 py-8 text-center">
          <p className="text-foreground/70">{t("empty")}</p>
          <p className="mt-2 text-sm text-foreground/60">
            <Link href="/venues" className="font-medium text-brand-primary hover:underline">
              {t("emptyVenuesLink")}
            </Link>
            {" · "}
            <Link href="/clubs" className="font-medium text-brand-primary hover:underline">
              {t("emptyClubsLink")}
            </Link>
          </p>
        </div>
      ) : (
        <>
          <ul className="flex flex-col gap-2">
            {items.map((a) => (
              <li key={a.id} className="rounded-2xl border border-brand-border bg-background px-5 py-4">
                <p className="text-foreground/90">
                  <ActivityLine activity={a} />
                </p>
                <span className="text-xs text-foreground/50">
                  {format.dateTime(new Date(a.createdAt), { dateStyle: "medium", timeStyle: "short" })}
                </span>
              </li>
            ))}
          </ul>

          {hasMore && (
            <button
              type="button"
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="cursor-pointer self-center rounded-full border border-brand-border px-5 py-2 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
            >
              {loadingMore ? t("loading") : t("loadMore")}
            </button>
          )}
        </>
      )}
    </div>
  );
}
