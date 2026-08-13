"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getVenueModerationQueue, hideVenue, publishVenue, type VenueListItem } from "@/lib/venuesApi";

export function VenuesQueue({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Moderation");
  const locale = useLocale();

  const [venues, setVenues] = useState<VenueListItem[] | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadQueue = useCallback(() => {
    getVenueModerationQueue(apiUrl, locale)
      .then(setVenues)
      .catch(() => setError(t("actionError")));
  }, [apiUrl, locale, t]);

  useEffect(() => {
    loadQueue();
  }, [loadQueue]);

  async function handleAction(id: string, action: "publish" | "hide") {
    setBusyId(id);
    setError(null);
    try {
      await (action === "publish" ? publishVenue(apiUrl, locale, id) : hideVenue(apiUrl, locale, id));
      setVenues((prev) => prev?.filter((v) => v.id !== id) ?? null);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {error && <p className="text-sm text-red-600">{error}</p>}

      {venues === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : venues.length === 0 ? (
        <p className="text-foreground/70">{t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {venues.map((venue) => (
            <li
              key={venue.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-5 py-4"
            >
              <div>
                <Link href={`/venues/${venue.slug}`} className="font-medium text-foreground hover:underline">
                  {venue.name}
                </Link>
                {venue.address && <p className="text-sm text-foreground/60">{venue.address}</p>}
              </div>
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => handleAction(venue.id, "publish")}
                  disabled={busyId === venue.id}
                  className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
                >
                  {busyId === venue.id ? t("publishing") : t("publish")}
                </button>
                <button
                  type="button"
                  onClick={() => handleAction(venue.id, "hide")}
                  disabled={busyId === venue.id}
                  className="cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
                >
                  {busyId === venue.id ? t("hiding") : t("hide")}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
