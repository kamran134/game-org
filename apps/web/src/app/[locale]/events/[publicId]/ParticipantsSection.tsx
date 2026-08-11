"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { addEventGuest, cancelEvent, joinEvent, leaveEvent, type EventDetail } from "@/lib/eventsApi";

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-sm text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

export function ParticipantsSection({ apiUrl, locale, event }: { apiUrl: string; locale: string; event: EventDetail }) {
  const t = useTranslations("Events");
  const router = useRouter();
  const [participants, setParticipants] = useState(event.participants);
  const [status, setStatus] = useState(event.status);
  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [guestName, setGuestName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const myParticipation = me ? participants.find((p) => p.userId === me.id) : undefined;
  const isCreator = me?.id === event.createdById;
  const cancelled = status === "Cancelled";
  const registrationClosed = event.registrationClosesAt ? new Date(event.registrationClosesAt) < new Date() : false;

  async function handleJoin(joinStatus: "Confirmed" | "Maybe") {
    setBusy(true);
    setError(null);
    try {
      const result = await joinEvent(apiUrl, locale, event.id, joinStatus);
      setParticipants((prev) => [...prev, result]);
    } catch (err) {
      // Заявка PendingApproval не входит в event.participants при перезагрузке
      // страницы (не светим ждущих одобрения всем подряд) — поэтому повторный
      // клик после релоада ловит именно эту ошибку с бэкенда, а не "успех".
      const message = err instanceof Error ? err.message : "";
      setError(event.requiresApproval && message.includes("уже записаны") ? t("requestAlreadyPending") : message || t("joinError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleLeave() {
    if (!myParticipation) return;
    setBusy(true);
    setError(null);
    try {
      await leaveEvent(apiUrl, locale, event.id);
      setParticipants((prev) => prev.filter((p) => p.id !== myParticipation.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : t("leaveError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleAddGuest() {
    if (!guestName.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const result = await addEventGuest(apiUrl, locale, event.id, guestName.trim());
      setParticipants((prev) => [...prev, result]);
      setGuestName("");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("guestError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleCancelEvent() {
    if (!confirm(t("cancelConfirm"))) return;
    setBusy(true);
    setError(null);
    try {
      await cancelEvent(apiUrl, locale, event.id);
      setStatus("Cancelled");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("cancelError"));
    } finally {
      setBusy(false);
    }
  }

  // Порядок в списке — Confirmed/Maybe первыми, Waitlisted (с номером очереди) последними.
  const ordered = [
    ...participants.filter((p) => p.status === "Confirmed"),
    ...participants.filter((p) => p.status === "Maybe"),
    ...participants.filter((p) => p.status === "Waitlisted").sort((a, b) => (a.waitlistOrder ?? 0) - (b.waitlistOrder ?? 0)),
  ];

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("participantsHeading")}</h2>
        {isCreator && !cancelled && (
          <button
            type="button"
            onClick={handleCancelEvent}
            disabled={busy}
            className="cursor-pointer text-sm text-red-600 transition-colors duration-200 hover:text-red-700 disabled:opacity-50"
          >
            {t("cancelEvent")}
          </button>
        )}
      </div>

      {cancelled && <p className="mt-3 text-sm text-red-600">{t("eventCancelledNotice")}</p>}

      {ordered.length === 0 ? (
        <p className="mt-3 text-sm text-foreground/60">{t("noParticipants")}</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-2">
          {ordered.map((p) => (
            <li key={p.id} className="flex items-center justify-between border-b border-brand-border pb-2 text-sm last:border-0">
              <span className="text-foreground">
                {p.displayName}
                {me && p.userId === me.id ? ` ${t("you")}` : ""}
              </span>
              <span className="text-foreground/50">{t(`statusOptions.${p.status}`)}</span>
            </li>
          ))}
        </ul>
      )}

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      {!cancelled && me === null && (
        <Link
          href="/login"
          className="mt-6 inline-block rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90"
        >
          {t("loginToJoin")}
        </Link>
      )}

      {!cancelled && me && registrationClosed && !myParticipation && (
        <p className="mt-6 text-sm text-foreground/60">{t("registrationClosed")}</p>
      )}

      {!cancelled && me && !registrationClosed && !myParticipation && (
        <div className="mt-6 flex gap-3">
          <button
            type="button"
            onClick={() => handleJoin("Confirmed")}
            disabled={busy}
            className="cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
          >
            {event.requiresApproval ? t("requestToJoin") : t("joinConfirmed")}
          </button>
          <button
            type="button"
            onClick={() => handleJoin("Maybe")}
            disabled={busy}
            className="cursor-pointer rounded-full border border-brand-border px-5 py-2 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-brand-primary/40 disabled:opacity-50"
          >
            {t("joinMaybe")}
          </button>
        </div>
      )}

      {!cancelled && me && myParticipation?.status === "PendingApproval" && (
        <div className="mt-6 flex items-center gap-3">
          <span className="text-sm text-foreground/60">{t("requestPending")}</span>
          <button
            type="button"
            onClick={handleLeave}
            disabled={busy}
            className="cursor-pointer text-sm text-red-600 transition-colors duration-200 hover:text-red-700 disabled:opacity-50"
          >
            {t("cancelRequest")}
          </button>
        </div>
      )}

      {!cancelled && me && myParticipation && myParticipation.status !== "PendingApproval" && (
        <button
          type="button"
          onClick={handleLeave}
          disabled={busy}
          className="mt-6 cursor-pointer text-sm text-red-600 transition-colors duration-200 hover:text-red-700 disabled:opacity-50"
        >
          {t("leave")}
        </button>
      )}

      {!cancelled && me && !registrationClosed && (
        <div className="mt-6 flex gap-2 border-t border-brand-border pt-6">
          <input
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            placeholder={t("guestNamePlaceholder")}
            maxLength={80}
            className={fieldClass}
          />
          <button
            type="button"
            onClick={handleAddGuest}
            disabled={busy || !guestName.trim()}
            className="shrink-0 cursor-pointer rounded-full border border-brand-border px-4 py-2 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-brand-primary/40 disabled:opacity-50"
          >
            {t("addGuest")}
          </button>
        </div>
      )}
    </section>
  );
}
