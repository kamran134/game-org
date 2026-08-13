"use client";

import { useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { CalendarBlank, MapPinLine, UsersThree } from "@phosphor-icons/react";
import { Link, useRouter } from "@/i18n/navigation";
import { buildFilterHref } from "@/lib/filterHref";
import { FilterSelect } from "@/components/filters/FilterSelect";
import { FilterPill } from "@/components/filters/FilterPill";
import { getEventsAuthed, type EventListItem, type EventType } from "@/lib/eventsApi";

// Единственный источник истины для всех фильтров — URL. Никакого локального
// состояния переключателей: любая комбинация вкладок выражается ссылкой,
// поэтому не бывает рассинхрона между тем, что в адресе, и тем, что
// нарисовано. Значения фильтров приходят пропами со страницы — она и так
// читает searchParams на сервере, клиентский хук тут лишний.
// "Мои"/"Мои клубы" грузятся на клиенте: SSR-запрос анонимный (как весь
// каталог), а тут нужна кука — тот же приём, что getEventAuthed для детальной.
export function EventsList({
  apiUrl,
  sportId,
  cityId,
  type,
  onlyFree,
  dateFrom,
  dateTo,
  upcoming,
  onlyMine,
  onlyMyClubs,
  query,
  initialEvents,
  sports,
  cities,
}: {
  apiUrl: string;
  sportId?: string;
  cityId?: string;
  type?: EventType;
  onlyFree?: boolean;
  dateFrom?: string;
  dateTo?: string;
  upcoming: boolean;
  onlyMine: boolean;
  onlyMyClubs: boolean;
  query: Record<string, string | undefined>;
  initialEvents: EventListItem[];
  sports: { id: string; name: string; emoji?: string | null }[];
  cities: { id: string; name: string }[];
}) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const format = useFormatter();
  const router = useRouter();

  const needsAuthed = onlyMine || onlyMyClubs;
  // Ключ набора фильтров: пока загруженный "Мои"/"Мои клубы" список
  // относится к другому набору, показываем загрузку, а не устаревшие данные.
  const filterKey = [
    upcoming ? "upcoming" : "past",
    sportId ?? "",
    cityId ?? "",
    type ?? "",
    onlyFree === undefined ? "" : String(onlyFree),
    dateFrom ?? "",
    dateTo ?? "",
    onlyMine ? "mine" : "",
    onlyMyClubs ? "myClubs" : "",
  ].join("|");

  const [authed, setAuthed] = useState<{ key: string; items: EventListItem[] } | null>(null);

  useEffect(() => {
    if (!needsAuthed) return;
    let cancelled = false;
    getEventsAuthed(apiUrl, locale, { sportId, cityId, upcoming, type, onlyFree, dateFrom, dateTo, onlyMine, onlyMyClubs })
      .then((items) => {
        if (!cancelled) setAuthed({ key: filterKey, items });
      })
      .catch(() => {
        if (!cancelled) setAuthed({ key: filterKey, items: [] });
      });
    return () => {
      cancelled = true;
    };
  }, [apiUrl, locale, sportId, cityId, type, onlyFree, dateFrom, dateTo, upcoming, onlyMine, onlyMyClubs, needsAuthed, filterKey]);

  const authedReady = authed !== null && authed.key === filterKey;
  const events = needsAuthed ? (authedReady ? authed.items : null) : initialEvents;

  function hrefWith(overrides: Record<string, string | null>): string {
    return buildFilterHref("/events", query, overrides);
  }

  const hasExtraFilters = !!(sportId || cityId || type || onlyFree !== undefined || dateFrom || dateTo || onlyMyClubs);

  const groups = events === null ? null : groupByDay(events, format);

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center gap-2 text-sm font-medium">
        <FilterPill href={hrefWith({ past: null })} active={upcoming}>
          {t("upcoming")}
        </FilterPill>
        <FilterPill href={hrefWith({ past: "1" })} active={!upcoming}>
          {t("past")}
        </FilterPill>
        <FilterPill href={hrefWith({ mine: onlyMine ? null : "1" })} active={onlyMine}>
          {t("onlyMine")}
        </FilterPill>
        <FilterPill href={hrefWith({ myClubs: onlyMyClubs ? null : "1" })} active={onlyMyClubs}>
          {t("onlyMyClubs")}
        </FilterPill>
      </div>

      <div className="mb-6 flex flex-wrap items-center gap-2">
        <FilterSelect
          value={sportId ?? ""}
          onChange={(v) => router.push(hrefWith({ sportId: v || null }))}
          placeholder={t("filters.sportAll")}
          options={sports.map((s) => ({ value: s.id, label: `${s.emoji ?? ""} ${s.name}`.trim() }))}
        />
        <FilterSelect
          value={cityId ?? ""}
          onChange={(v) => router.push(hrefWith({ cityId: v || null }))}
          placeholder={t("filters.cityAll")}
          options={cities.map((c) => ({ value: c.id, label: c.name }))}
        />
        <FilterSelect
          value={type ?? ""}
          onChange={(v) => router.push(hrefWith({ type: v || null }))}
          placeholder={t("filters.typeAll")}
          options={(["Game", "Training", "Tournament", "Friendly"] as EventType[]).map((ty) => ({ value: ty, label: t(`typeOptions.${ty}`) }))}
        />
        <FilterSelect
          value={onlyFree === undefined ? "" : onlyFree ? "free" : "paid"}
          onChange={(v) => router.push(hrefWith({ onlyFree: v === "free" ? "true" : v === "paid" ? "false" : null }))}
          placeholder={t("filters.costAll")}
          options={[
            { value: "free", label: t("filters.costFree") },
            { value: "paid", label: t("filters.costPaid") },
          ]}
        />
        <label className="flex items-center gap-1.5 text-sm text-foreground/60">
          {t("filters.dateFrom")}
          <input
            type="date"
            value={dateFrom ?? ""}
            onChange={(e) => router.push(hrefWith({ dateFrom: e.target.value || null }))}
            className="rounded-full border border-brand-border bg-background px-3 py-1.5 text-sm text-foreground"
          />
        </label>
        <label className="flex items-center gap-1.5 text-sm text-foreground/60">
          {t("filters.dateTo")}
          <input
            type="date"
            value={dateTo ?? ""}
            onChange={(e) => router.push(hrefWith({ dateTo: e.target.value || null }))}
            className="rounded-full border border-brand-border bg-background px-3 py-1.5 text-sm text-foreground"
          />
        </label>
      </div>

      {events === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : events.length === 0 ? (
        hasExtraFilters ? (
          <div className="flex flex-col items-start gap-2">
            <p className="text-foreground/70">{t("emptyFiltered")}</p>
            <Link href="/events" className="text-sm font-medium text-brand-primary hover:underline">
              {t("resetFilters")}
            </Link>
          </div>
        ) : (
          <p className="text-foreground/70">{onlyMine ? t("emptyMine") : t("empty")}</p>
        )
      ) : (
        <div className="flex flex-col gap-6">
          {groups!.map((group) => (
            <div key={group.key}>
              <div className="sticky top-14 z-10 -mx-2 bg-brand-background/95 px-2 py-2 backdrop-blur-sm">
                <p className="text-sm font-semibold capitalize text-foreground/70">{group.label}</p>
              </div>
              <ul className="mt-2 flex flex-col gap-3">
                {group.items.map((ev) => {
                  const place = ev.venue?.name ?? ev.customLocation;
                  const spotsLeft = ev.maxParticipants != null ? ev.maxParticipants - ev.confirmedCount : null;
                  return (
                    <li key={ev.id}>
                      <Link
                        href={`/events/${ev.publicId}`}
                        className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                      >
                        <div>
                          <p className="flex flex-wrap items-center gap-2 font-medium text-foreground">
                            {ev.sport.emoji} {ev.title ?? ev.sport.nameI18n[locale] ?? ev.sport.slug}
                            {ev.requiresApproval && (
                              <span className="rounded-full bg-brand-accent/10 px-2 py-0.5 text-xs font-medium text-brand-accent">
                                {t("requiresApprovalBadge")}
                              </span>
                            )}
                          </p>
                          <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                            <CalendarBlank size={14} weight="bold" />
                            {format.dateTime(new Date(ev.startsAt), { timeStyle: "short" })}
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
                          {ev.cost != null && ev.cost > 0 && (
                            <span>
                              {ev.cost} {ev.currency}
                            </span>
                          )}
                        </div>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </div>
      )}
    </>
  );
}

type DayGroup = { key: string; label: string; items: EventListItem[] };

// Список уже приходит с бэкенда отсортированным по startsAt (Шаг 24) —
// достаточно схлопывать подряд идущие элементы одного календарного дня,
// не строя отдельную карту группировки.
function groupByDay(events: EventListItem[], format: ReturnType<typeof useFormatter>): DayGroup[] {
  const groups: DayGroup[] = [];
  for (const ev of events) {
    const date = new Date(ev.startsAt);
    const key = date.toDateString();
    const last = groups[groups.length - 1];
    if (last && last.key === key) {
      last.items.push(ev);
    } else {
      groups.push({ key, label: format.dateTime(date, { weekday: "long", day: "numeric", month: "long" }), items: [ev] });
    }
  }
  return groups;
}
