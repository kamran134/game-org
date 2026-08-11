"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { setEventTeams, type EventDetail, type EventTeam, type TeamInput } from "@/lib/eventsApi";

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-sm text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

function toInputs(teams: EventTeam[]): TeamInput[] {
  return teams
    .slice()
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((t) => ({ name: t.name, colorHex: t.colorHex, sortOrder: t.sortOrder, participantIds: t.memberParticipantIds }));
}

/**
 * Редактор команд (создатель/модератор, пока событие не Completed — после
 * записи результата состав не трогаем) + всегда видимый ростер (тем более
 * когда событие уже Completed и это часть отображения результата).
 */
export function TeamsSection({ apiUrl, locale, event }: { apiUrl: string; locale: string; event: EventDetail }) {
  const t = useTranslations("Events");
  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [teams, setTeams] = useState(event.teams);
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<TeamInput[]>(() => toInputs(event.teams));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === event.createdById || me.role === "Moderator" || me.role === "Admin");
  const canEdit = canManage && event.status !== "Completed";
  const participants = event.participants;

  function startEditing() {
    setDraft(toInputs(teams));
    setEditing(true);
    setError(null);
  }

  function addTeam() {
    setDraft((prev) => [...prev, { name: t("teams.newTeamName", { count: prev.length + 1 }), colorHex: null, sortOrder: prev.length, participantIds: [] }]);
  }

  function removeTeam(index: number) {
    setDraft((prev) => prev.filter((_, i) => i !== index));
  }

  function updateName(index: number, name: string) {
    setDraft((prev) => prev.map((team, i) => (i === index ? { ...team, name } : team)));
  }

  function updateColor(index: number, colorHex: string) {
    setDraft((prev) => prev.map((team, i) => (i === index ? { ...team, colorHex: colorHex || null } : team)));
  }

  function toggleMember(index: number, participantId: string) {
    setDraft((prev) =>
      prev.map((team, i) => {
        const isMember = team.participantIds.includes(participantId);
        if (i === index) {
          return { ...team, participantIds: isMember ? team.participantIds.filter((id) => id !== participantId) : [...team.participantIds, participantId] };
        }
        // Участник не может быть в двух командах — снимаем из чужой, если поставили в эту.
        return isMember ? team : { ...team, participantIds: team.participantIds.filter((id) => id !== participantId) };
      }),
    );
  }

  async function handleSave() {
    setBusy(true);
    setError(null);
    try {
      await setEventTeams(apiUrl, locale, event.id, { teams: draft });
      setTeams(
        draft.map((d, i) => ({ id: teams[i]?.id ?? `${i}`, name: d.name, colorHex: d.colorHex, score: null, sortOrder: d.sortOrder, memberParticipantIds: d.participantIds })),
      );
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("teams.saveError"));
    } finally {
      setBusy(false);
    }
  }

  if (!canEdit && teams.length === 0) return null;

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("teams.heading")}</h2>
        {canEdit && !editing && (
          <button type="button" onClick={startEditing} className="cursor-pointer text-sm font-medium text-brand-primary hover:underline">
            {teams.length > 0 ? t("teams.edit") : t("teams.setUp")}
          </button>
        )}
      </div>

      {!editing && teams.length === 0 && <p className="mt-3 text-sm text-foreground/60">{t("teams.none")}</p>}

      {!editing && teams.length > 0 && (
        <ul className="mt-4 flex flex-col gap-4">
          {teams
            .slice()
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((team) => (
              <li key={team.id} className="rounded-xl border border-brand-border p-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 font-medium text-foreground">
                    {team.colorHex && <span className="h-3 w-3 rounded-full" style={{ backgroundColor: team.colorHex }} />}
                    {team.name}
                  </span>
                  {team.score != null && <span className="text-lg font-semibold text-foreground">{team.score}</span>}
                </div>
                <p className="mt-2 text-sm text-foreground/60">
                  {participants
                    .filter((p) => team.memberParticipantIds.includes(p.id))
                    .map((p) => p.displayName)
                    .join(", ") || t("teams.noMembers")}
                </p>
              </li>
            ))}
        </ul>
      )}

      {editing && (
        <div className="mt-4 flex flex-col gap-4">
          {draft.map((team, index) => (
            <div key={index} className="rounded-xl border border-brand-border p-4">
              <div className="flex items-center gap-2">
                <input value={team.name} onChange={(e) => updateName(index, e.target.value)} maxLength={40} className={fieldClass} />
                <input
                  type="color"
                  value={team.colorHex ?? "#888888"}
                  onChange={(e) => updateColor(index, e.target.value)}
                  className="h-9 w-9 shrink-0 cursor-pointer rounded-lg border border-brand-border bg-background"
                />
                <button
                  type="button"
                  onClick={() => removeTeam(index)}
                  className="shrink-0 cursor-pointer text-sm text-red-600 transition-colors duration-200 hover:underline dark:text-red-400"
                >
                  {t("teams.remove")}
                </button>
              </div>
              <div className="mt-3 flex flex-wrap gap-3">
                {participants.map((p) => (
                  <label key={p.id} className="flex cursor-pointer items-center gap-1.5 text-sm text-foreground/80">
                    <input type="checkbox" checked={team.participantIds.includes(p.id)} onChange={() => toggleMember(index, p.id)} />
                    {p.displayName}
                  </label>
                ))}
              </div>
            </div>
          ))}

          <div className="flex items-center gap-3">
            <button type="button" onClick={addTeam} className="cursor-pointer text-sm font-medium text-brand-primary hover:underline">
              {t("teams.addTeam")}
            </button>
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleSave}
              disabled={busy}
              className="cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
            >
              {busy ? t("teams.saving") : t("teams.save")}
            </button>
            <button
              type="button"
              onClick={() => setEditing(false)}
              className="cursor-pointer text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground"
            >
              {t("teams.cancel")}
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
