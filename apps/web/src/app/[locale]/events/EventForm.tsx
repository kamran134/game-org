"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getMyClubs, type ClubListItem } from "@/lib/clubsApi";
import {
  createEvent,
  updateEvent,
  type CostSplit,
  type CreateEventRequest,
  type EventDetail,
  type EventType,
  type EventVisibility,
} from "@/lib/eventsApi";
import { EMPTY_LOCALIZED_TEXT, type LocalizedText } from "@/lib/localized";
import { I18nField } from "@/components/I18nField";

type Option = { id: string; name: string };

const TYPES: EventType[] = ["Game", "Training", "Tournament", "Friendly"];
const ALL_VISIBILITIES: EventVisibility[] = ["Public", "Club", "Unlisted"];
const COST_SPLITS: CostSplit[] = ["Free", "PerPlayer", "Total"];

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

// datetime-local работает в "наивном" локальном времени браузера — тот же
// Date, что заполняет поле, читает его обратно, так что раунд-трип
// консистентен без явной обработки часовых поясов.
function toDatetimeLocalValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function EventForm({
  apiUrl,
  sports,
  venues,
  mode,
  event,
}: {
  apiUrl: string;
  sports: Option[];
  venues: Option[];
  mode: "create" | "edit";
  event?: EventDetail;
}) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const router = useRouter();

  const [checking, setChecking] = useState(true);
  const [allowed, setAllowed] = useState(mode === "create");

  const [type, setType] = useState<EventType>(event?.type ?? "Game");
  const [visibility, setVisibility] = useState<EventVisibility>(event?.visibility ?? "Public");
  const [clubId, setClubId] = useState(event?.clubId ?? "");
  const [myClubs, setMyClubs] = useState<ClubListItem[]>([]);
  const [sportId, setSportId] = useState(event?.sport.id ?? sports[0]?.id ?? "");
  const [venueId, setVenueId] = useState(event?.venue?.id ?? "");
  const [customLocation, setCustomLocation] = useState(event?.customLocation ?? "");
  const [title, setTitle] = useState<LocalizedText>(event?.titleI18n ?? EMPTY_LOCALIZED_TEXT);
  const [description, setDescription] = useState<LocalizedText>(event?.descriptionI18n ?? EMPTY_LOCALIZED_TEXT);
  const [startsAt, setStartsAt] = useState(event ? toDatetimeLocalValue(event.startsAt) : "");
  const [endsAt, setEndsAt] = useState(event ? toDatetimeLocalValue(event.endsAt) : "");
  const [minParticipants, setMinParticipants] = useState(event?.minParticipants?.toString() ?? "");
  const [maxParticipants, setMaxParticipants] = useState(event?.maxParticipants?.toString() ?? "");
  const [waitlistEnabled, setWaitlistEnabled] = useState(event?.waitlistEnabled ?? true);
  const [costSplit, setCostSplit] = useState<CostSplit>(event?.costSplit ?? "Free");
  const [cost, setCost] = useState(event?.cost?.toString() ?? "");
  const [currency, setCurrency] = useState(event?.currency ?? "AZN");
  const [lockHoursBeforeStart, setLockHoursBeforeStart] = useState(event?.lockHoursBeforeStart?.toString() ?? "");

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then((me: MeProfile | null) => {
        if (!me) {
          router.push("/login");
          return;
        }
        setAllowed(mode === "create" || !event || me.id === event.createdById);
        setChecking(false);
      })
      .catch(() => setChecking(false));
  }, [apiUrl, locale, mode, router, event]);

  useEffect(() => {
    getMyClubs(apiUrl, locale).then(setMyClubs);
  }, [apiUrl, locale]);

  // ClubId неизменяем после создания (см. UpdateEventRequest на бэкенде) —
  // видимость "Клуб" доступна только там, где есть из чего выбрать: при
  // создании — если состоишь хоть в одном клубе, при редактировании — если
  // клуб уже был привязан.
  const visibilities: EventVisibility[] =
    mode === "create"
      ? myClubs.length > 0
        ? ALL_VISIBILITIES
        : ["Public", "Unlisted"]
      : event?.clubId
        ? ALL_VISIBILITIES
        : ["Public", "Unlisted"];

  const hasPlace = venueId !== "" || customLocation.trim() !== "";

  async function handleSubmit() {
    setSaving(true);
    setError(null);
    try {
      const body: CreateEventRequest = {
        type,
        visibility,
        sportId,
        clubId: clubId || null,
        venueId: venueId || null,
        customLocation: customLocation.trim() || null,
        title,
        description,
        startsAt: new Date(startsAt).toISOString(),
        endsAt: new Date(endsAt).toISOString(),
        minParticipants: minParticipants ? Number(minParticipants) : null,
        maxParticipants: maxParticipants ? Number(maxParticipants) : null,
        waitlistEnabled,
        costSplit,
        cost: costSplit !== "Free" && cost ? Number(cost) : null,
        currency,
        lockHoursBeforeStart: lockHoursBeforeStart ? Number(lockHoursBeforeStart) : null,
      };

      if (mode === "create") {
        const created = await createEvent(apiUrl, locale, body);
        router.push(`/events/${created.publicId}`);
      } else if (event) {
        await updateEvent(apiUrl, locale, event.id, body);
        router.push(`/events/${event.publicId}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : mode === "create" ? t("createError") : t("saveError"));
    } finally {
      setSaving(false);
    }
  }

  if (checking) return null;
  if (!allowed) return <p className="text-red-600">{t("noAccessEdit")}</p>;

  return (
    <div className="flex flex-col gap-4 rounded-2xl border border-brand-border bg-background p-8">
      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.type")}</span>
          <select className={fieldClass} value={type} onChange={(e) => setType(e.target.value as EventType)}>
            {TYPES.map((ty) => (
              <option key={ty} value={ty}>
                {t(`typeOptions.${ty}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.visibility")}</span>
          <select className={fieldClass} value={visibility} onChange={(e) => setVisibility(e.target.value as EventVisibility)}>
            {visibilities.map((v) => (
              <option key={v} value={v}>
                {t(`visibilityOptions.${v}`)}
              </option>
            ))}
          </select>
        </label>
      </div>

      {visibility === "Club" && (
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.club")}</span>
          <select className={fieldClass} value={clubId} onChange={(e) => setClubId(e.target.value)} disabled={mode === "edit"}>
            <option value="">{t("fields.clubNotSet")}</option>
            {myClubs.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
      )}

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.sport")}</span>
        <select className={fieldClass} value={sportId} onChange={(e) => setSportId(e.target.value)} disabled={mode === "edit"}>
          <option value="">{t("fields.sportNotSet")}</option>
          {sports.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.venue")}</span>
        <select className={fieldClass} value={venueId} onChange={(e) => setVenueId(e.target.value)}>
          <option value="">{t("fields.venueNotSet")}</option>
          {venues.map((v) => (
            <option key={v.id} value={v.id}>
              {v.name}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.customLocation")}</span>
        <input
          className={fieldClass}
          value={customLocation}
          onChange={(e) => setCustomLocation(e.target.value)}
          maxLength={200}
        />
      </label>
      {!hasPlace && <p className="text-sm text-red-600">{t("placeRequired")}</p>}

      <I18nField label={t("fields.title")} value={title} onChange={setTitle} maxLength={120} />
      <I18nField label={t("fields.description")} value={description} onChange={setDescription} multiline maxLength={2000} />

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.startsAt")}</span>
          <input
            type="datetime-local"
            className={fieldClass}
            value={startsAt}
            onChange={(e) => setStartsAt(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.endsAt")}</span>
          <input type="datetime-local" className={fieldClass} value={endsAt} onChange={(e) => setEndsAt(e.target.value)} />
        </label>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.minParticipants")}</span>
          <input
            type="number"
            className={fieldClass}
            value={minParticipants}
            onChange={(e) => setMinParticipants(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.maxParticipants")}</span>
          <input
            type="number"
            className={fieldClass}
            value={maxParticipants}
            onChange={(e) => setMaxParticipants(e.target.value)}
          />
        </label>
      </div>

      <label className="flex items-center gap-2 text-sm text-foreground">
        <input type="checkbox" checked={waitlistEnabled} onChange={(e) => setWaitlistEnabled(e.target.checked)} />
        {t("fields.waitlistEnabled")}
      </label>

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.costSplit")}</span>
          <select className={fieldClass} value={costSplit} onChange={(e) => setCostSplit(e.target.value as CostSplit)}>
            {COST_SPLITS.map((c) => (
              <option key={c} value={c}>
                {t(`costSplitOptions.${c}`)}
              </option>
            ))}
          </select>
        </label>
        {costSplit !== "Free" && (
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-foreground">{t("fields.cost")}</span>
            <input type="number" className={fieldClass} value={cost} onChange={(e) => setCost(e.target.value)} />
          </label>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.currency")}</span>
          <input className={fieldClass} value={currency} onChange={(e) => setCurrency(e.target.value)} maxLength={3} />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.lockHoursBeforeStart")}</span>
          <input
            type="number"
            className={fieldClass}
            value={lockHoursBeforeStart}
            onChange={(e) => setLockHoursBeforeStart(e.target.value)}
          />
        </label>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button
        type="button"
        onClick={handleSubmit}
        disabled={saving || !sportId || !hasPlace || !startsAt || !endsAt}
        className="self-start rounded-full bg-brand-primary px-6 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50 cursor-pointer"
      >
        {saving ? (mode === "create" ? t("creating") : t("saving")) : mode === "create" ? t("create") : t("save")}
      </button>
    </div>
  );
}
