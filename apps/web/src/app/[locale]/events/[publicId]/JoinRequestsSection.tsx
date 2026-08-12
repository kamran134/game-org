"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import {
  approveEventJoinRequest,
  getEventJoinRequests,
  rejectEventJoinRequest,
  type EventDetail,
  type EventJoinRequestItem,
} from "@/lib/eventsApi";

export function JoinRequestsSection({ apiUrl, event }: { apiUrl: string; event: EventDetail }) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [requests, setRequests] = useState<EventJoinRequestItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === event.createdById || me.role === "Moderator" || me.role === "Admin");

  useEffect(() => {
    if (!canManage) return;
    getEventJoinRequests(apiUrl, locale, event.id)
      .then(setRequests)
      .catch((err) => setError(err instanceof Error ? err.message : t("joinRequests.loadError")));
  }, [apiUrl, locale, event.id, canManage, t]);

  if (!event.requiresApproval || !canManage) return null;

  async function handleDecision(participantId: string, action: "approve" | "reject") {
    setBusyId(participantId);
    setError(null);
    try {
      if (action === "approve") await approveEventJoinRequest(apiUrl, locale, event.id, participantId);
      else await rejectEventJoinRequest(apiUrl, locale, event.id, participantId);
      setRequests((prev) => prev?.filter((r) => r.participantId !== participantId) ?? null);
      // ParticipantsSection — сосед на этой же странице, свой useState посеян
      // из event.participants один раз при монтировании; без этого approve
      // не долетает до списка участников, пока страницу не перезагрузят руками.
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("joinRequests.actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section id="requests" className="rounded-2xl border border-brand-border bg-background p-8">
      <h2 className="font-heading text-lg font-semibold text-foreground">{t("joinRequests.heading")}</h2>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      {requests === null ? (
        <p className="mt-3 text-sm text-foreground/60">{t("joinRequests.loading")}</p>
      ) : requests.length === 0 ? (
        <p className="mt-3 text-sm text-foreground/60">{t("joinRequests.none")}</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-2">
          {requests.map((r) => (
            <li key={r.participantId} className="flex items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-5 py-4">
              <span className="font-medium text-foreground">{r.displayName}</span>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => handleDecision(r.participantId, "approve")}
                  disabled={busyId === r.participantId}
                  className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
                >
                  {t("joinRequests.approve")}
                </button>
                <button
                  type="button"
                  onClick={() => handleDecision(r.participantId, "reject")}
                  disabled={busyId === r.participantId}
                  className="cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-brand-primary/40 disabled:opacity-50"
                >
                  {t("joinRequests.reject")}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
