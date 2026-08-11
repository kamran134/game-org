"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import {
  recordResult,
  type AttendanceStatus,
  type EventDetail,
  type StandingEntry,
  type TeamScoreEntry,
} from "@/lib/eventsApi";

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-sm text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

const ATTENDANCE_OPTIONS: AttendanceStatus[] = ["Attended", "NoShow", "LateCancel"];

/** Только для создателя/модератора, только после Completed — форма один раз, повторно можно редактировать. */
export function RecordResultSection({ apiUrl, locale, event }: { apiUrl: string; locale: string; event: EventDetail }) {
  const t = useTranslations("Events");
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [summary, setSummary] = useState(event.result?.summary ?? "");
  const [teamScores, setTeamScores] = useState<Record<string, string>>(
    Object.fromEntries(event.teams.map((team) => [team.id, team.score != null ? String(team.score) : ""])),
  );
  const [places, setPlaces] = useState<Record<string, string>>({});
  const [attendance, setAttendance] = useState<Record<string, AttendanceStatus | "">>(
    Object.fromEntries(
      event.participants
        .filter((p) => ATTENDANCE_OPTIONS.includes(p.status as AttendanceStatus))
        .map((p) => [p.id, p.status as AttendanceStatus]),
    ),
  );
  const [mvpUserId, setMvpUserId] = useState(event.result?.mvpUserId ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === event.createdById || me.role === "Moderator" || me.role === "Admin");
  if (!canManage || event.status !== "Completed") return null;

  const hasTeams = event.teams.length > 0;
  const userParticipants = event.participants.filter((p) => p.userId);
  const autoMvpName = event.mvpTally[0]?.displayName;

  async function handleSubmit() {
    setBusy(true);
    setError(null);
    try {
      const teamScoreEntries: TeamScoreEntry[] = Object.entries(teamScores)
        .filter(([, value]) => value.trim() !== "")
        .map(([teamId, value]) => ({ teamId, score: Number(value) }));

      const standings: StandingEntry[] = Object.entries(places)
        .filter(([, value]) => value.trim() !== "")
        .map(([userId, value]) => ({ userId, place: Number(value) }));

      const attendanceEntries = Object.entries(attendance)
        .filter((entry): entry is [string, AttendanceStatus] => entry[1] !== "")
        .map(([participantId, status]) => ({ participantId, status }));

      await recordResult(apiUrl, locale, event.id, {
        summary: summary.trim() || null,
        teamScores: hasTeams ? teamScoreEntries : null,
        standings: hasTeams ? null : standings,
        attendance: attendanceEntries,
        mvpUserId: mvpUserId || null,
      });
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("result.saveError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <h2 className="font-heading text-lg font-semibold text-foreground">{event.result ? t("result.editHeading") : t("result.recordHeading")}</h2>

      <label className="mt-4 flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("result.summary")}</span>
        <textarea value={summary} onChange={(e) => setSummary(e.target.value)} rows={3} maxLength={500} className={fieldClass} />
      </label>

      {hasTeams ? (
        <div className="mt-4 flex flex-col gap-2">
          <span className="text-sm font-medium text-foreground">{t("result.teamScores")}</span>
          {event.teams.map((team) => (
            <div key={team.id} className="flex items-center gap-3">
              <span className="w-40 text-sm text-foreground/80">{team.name}</span>
              <input
                type="number"
                value={teamScores[team.id] ?? ""}
                onChange={(e) => setTeamScores((prev) => ({ ...prev, [team.id]: e.target.value }))}
                className={fieldClass}
              />
            </div>
          ))}
        </div>
      ) : (
        <div className="mt-4 flex flex-col gap-2">
          <span className="text-sm font-medium text-foreground">{t("result.standings")}</span>
          {userParticipants.map((p) => (
            <div key={p.id} className="flex items-center gap-3">
              <span className="w-40 text-sm text-foreground/80">{p.displayName}</span>
              <input
                type="number"
                placeholder={t("result.placePlaceholder")}
                value={places[p.userId!] ?? ""}
                onChange={(e) => setPlaces((prev) => ({ ...prev, [p.userId!]: e.target.value }))}
                className={fieldClass}
              />
            </div>
          ))}
        </div>
      )}

      <div className="mt-4 flex flex-col gap-2">
        <span className="text-sm font-medium text-foreground">{t("result.attendance")}</span>
        {event.participants.map((p) => (
          <div key={p.id} className="flex items-center gap-3">
            <span className="w-40 text-sm text-foreground/80">{p.displayName}</span>
            <select
              value={attendance[p.id] ?? ""}
              onChange={(e) => setAttendance((prev) => ({ ...prev, [p.id]: e.target.value as AttendanceStatus | "" }))}
              className={fieldClass}
            >
              <option value="">{t("result.attendanceUnset")}</option>
              {ATTENDANCE_OPTIONS.map((opt) => (
                <option key={opt} value={opt}>
                  {t(`statusOptions.${opt}`)}
                </option>
              ))}
            </select>
          </div>
        ))}
      </div>

      <label className="mt-4 flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("result.mvp")}</span>
        <select value={mvpUserId} onChange={(e) => setMvpUserId(e.target.value)} className={fieldClass}>
          <option value="">{autoMvpName ? t("result.mvpAuto", { name: autoMvpName }) : t("result.mvpNone")}</option>
          {userParticipants.map((p) => (
            <option key={p.userId} value={p.userId!}>
              {p.displayName}
            </option>
          ))}
        </select>
      </label>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      <button
        type="button"
        onClick={handleSubmit}
        disabled={busy}
        className="mt-4 cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
      >
        {busy ? t("result.saving") : t("result.save")}
      </button>
    </section>
  );
}
