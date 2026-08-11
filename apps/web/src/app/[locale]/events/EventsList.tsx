"use client";

import { useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { CalendarBlank, MapPinLine, UsersThree } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { getEventsAuthed, type EventListItem } from "@/lib/eventsApi";

// "Мои" — клиентский дозапрос с cookie (getEventsAuthed), потому что сама
// страница списка SSR-анонимна (как весь остальной каталог). Переключатель
// живёт тут, а не в page.tsx, именно поэтому — там нет доступа к cookie.
export function EventsList({
  apiUrl,
  sportId,
  upcoming,
  initialEvents,
}: {
  apiUrl: string;
  sportId?: string;
  upcoming: boolean;
  initialEvents: EventListItem[];
}) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const format = useFormatter();

  const [onlyMine, setOnlyMine] = useState(false);
  const [events, setEvents] = useState(initialEvents);
  const [loading, setLoading] = useState(false);

  async function toggleMine() {
    const next = !onlyMine;
    setOnlyMine(next);
    if (!next) {
      setEvents(initialEvents);
      return;
    }
    setLoading(true);
    try {
      setEvents(await getEventsAuthed(apiUrl, locale, { sportId, upcoming, onlyMine: true }));
    } catch {
      setEvents([]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center gap-2 text-sm font-medium">
        <Link
          href="/events"
          className={`rounded-full border px-4 py-1.5 transition-colors duration-200 ${
            upcoming && !onlyMine
              ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
              : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
          }`}
        >
          {t("upcoming")}
        </Link>
        <Link
          href="/events?past=1"
          className={`rounded-full border px-4 py-1.5 transition-colors duration-200 ${
            !upcoming && !onlyMine
              ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
              : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
          }`}
        >
          {t("past")}
        </Link>
        <button
          type="button"
          onClick={toggleMine}
          className={`cursor-pointer rounded-full border px-4 py-1.5 transition-colors duration-200 ${
            onlyMine
              ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
              : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
          }`}
        >
          {t("onlyMine")}
        </button>
      </div>

      {loading ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : events.length === 0 ? (
        <p className="text-foreground/70">{onlyMine ? t("emptyMine") : t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {events.map((ev) => {
            const place = ev.venue?.name ?? ev.customLocation;
            const spotsLeft = ev.maxParticipants != null ? ev.maxParticipants - ev.confirmedCount : null;
            return (
              <li key={ev.id}>
                <Link
                  href={`/events/${ev.publicId}`}
                  className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <div>
                    <p className="font-medium text-foreground">
                      {ev.sport.emoji} {ev.title ?? ev.sport.nameI18n[locale] ?? ev.sport.slug}
                    </p>
                    <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                      <CalendarBlank size={14} weight="bold" />
                      {format.dateTime(new Date(ev.startsAt), { dateStyle: "medium", timeStyle: "short" })}
                    </p>
                    {place && (
                      <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                        <MapPinLine size={14} weight="bold" />
                        {place}
                      </p>
                    )}
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1 text-sm text-foreground/60">
                    {ev.status === "Cancelled" ? (
                      <span className="text-red-600">{t("eventCancelledNotice")}</span>
                    ) : (
                      <span className="flex items-center gap-1">
                        <UsersThree size={14} weight="bold" />
                        {spotsLeft != null ? (spotsLeft > 0 ? t("spotsLeft", { count: spotsLeft }) : t("full")) : ev.confirmedCount}
                      </span>
                    )}
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </>
  );
}
