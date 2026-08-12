"use client";

import { useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { CalendarBlank, MapPinLine, UsersThree } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { getEventsAuthed, type EventListItem } from "@/lib/eventsApi";

// Единственный источник истины для всех фильтров — URL (?past=1&mine=1).
// Никакого локального состояния переключателей: любая комбинация вкладок
// выражается ссылкой, поэтому не бывает рассинхрона между тем, что в адресе,
// и тем, что нарисовано. Список "Мои" грузится на клиенте — SSR-запрос
// анонимный (как весь каталог), а тут нужна кука; всё остальное приходит
// готовым с сервера первым рендером.
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
  const searchParams = useSearchParams();

  const onlyMine = searchParams.get("mine") === "1";
  // Ключ набора фильтров: пока загруженные "Мои" относятся к другому набору,
  // показываем загрузку, а не устаревший список от прошлой вкладки.
  const filterKey = `${upcoming ? "upcoming" : "past"}|${sportId ?? ""}`;
  const [mine, setMine] = useState<{ key: string; items: EventListItem[] } | null>(null);

  useEffect(() => {
    if (!onlyMine) return;
    let cancelled = false;
    getEventsAuthed(apiUrl, locale, { sportId, upcoming, onlyMine: true })
      .then((items) => {
        if (!cancelled) setMine({ key: filterKey, items });
      })
      .catch(() => {
        if (!cancelled) setMine({ key: filterKey, items: [] });
      });
    return () => {
      cancelled = true;
    };
  }, [apiUrl, locale, sportId, upcoming, onlyMine, filterKey]);

  const mineReady = mine !== null && mine.key === filterKey;
  const events = onlyMine ? (mineReady ? mine.items : null) : initialEvents;

  function hrefWith(overrides: Record<string, string | null>): string {
    const next = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(overrides)) {
      if (value === null) next.delete(key);
      else next.set(key, value);
    }
    const qs = next.toString();
    return qs ? `/events?${qs}` : "/events";
  }

  const tabClass = (active: boolean) =>
    `rounded-full border px-4 py-1.5 transition-colors duration-200 ${
      active
        ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
        : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
    }`;

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center gap-2 text-sm font-medium">
        <Link href={hrefWith({ past: null })} className={tabClass(upcoming)}>
          {t("upcoming")}
        </Link>
        <Link href={hrefWith({ past: "1" })} className={tabClass(!upcoming)}>
          {t("past")}
        </Link>
        <Link href={hrefWith({ mine: onlyMine ? null : "1" })} className={tabClass(onlyMine)}>
          {t("onlyMine")}
        </Link>
      </div>

      {events === null ? (
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
