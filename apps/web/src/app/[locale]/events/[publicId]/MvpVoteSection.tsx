"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { voteMvp, type EventDetail, type MvpTallyEntry } from "@/lib/eventsApi";

export function MvpVoteSection({ apiUrl, event }: { apiUrl: string; event: EventDetail }) {
  const t = useTranslations("Events");
  const locale = useLocale();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [myVote, setMyVote] = useState(event.myMvpVote ?? null);
  const [tally, setTally] = useState<MvpTallyEntry[]>(event.mvpTally);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  if (event.status !== "Completed") return null;

  const myParticipation = me ? event.participants.find((p) => p.userId === me.id) : undefined;
  const canVote = !!myParticipation;
  const candidates = event.participants.filter((p) => p.userId && p.userId !== me?.id);

  async function handleVote(targetUserId: string) {
    setBusy(true);
    setError(null);
    try {
      await voteMvp(apiUrl, locale, event.id, targetUserId);
      setTally((prev) => {
        const withoutMyOldVote = myVote
          ? prev.map((e) => (e.userId === myVote ? { ...e, votes: e.votes - 1 } : e)).filter((e) => e.votes > 0)
          : prev;
        const existing = withoutMyOldVote.find((e) => e.userId === targetUserId);
        const updated = existing
          ? withoutMyOldVote.map((e) => (e.userId === targetUserId ? { ...e, votes: e.votes + 1 } : e))
          : [...withoutMyOldVote, { userId: targetUserId, displayName: candidates.find((c) => c.userId === targetUserId)?.displayName ?? "", votes: 1 }];
        return updated.sort((a, b) => b.votes - a.votes);
      });
      setMyVote(targetUserId);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("mvp.voteError"));
    } finally {
      setBusy(false);
    }
  }

  if (!canVote && tally.length === 0) return null;

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <h2 className="font-heading text-lg font-semibold text-foreground">{t("mvp.heading")}</h2>

      {tally.length > 0 && (
        <ul className="mt-3 flex flex-col gap-1.5">
          {tally.map((entry) => (
            <li key={entry.userId} className="flex items-center justify-between text-sm">
              <span className={entry.userId === myVote ? "font-medium text-brand-primary" : "text-foreground/80"}>{entry.displayName}</span>
              <span className="text-foreground/50">{t("mvp.votesCount", { count: entry.votes })}</span>
            </li>
          ))}
        </ul>
      )}

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      {canVote && candidates.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {candidates.map((c) => (
            <button
              key={c.id}
              type="button"
              onClick={() => handleVote(c.userId!)}
              disabled={busy}
              className={
                c.userId === myVote
                  ? "cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground disabled:opacity-50"
                  : "cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-brand-primary/40 disabled:opacity-50"
              }
            >
              {c.displayName}
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
