"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { completeEvent, deleteEvent, type EventStatus } from "@/lib/eventsApi";

export function EventActions({
  apiUrl,
  eventId,
  createdById,
  status,
  endsAt,
}: {
  apiUrl: string;
  eventId: string;
  createdById?: string | null;
  status: EventStatus;
  endsAt: string;
}) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [deleting, setDeleting] = useState(false);
  const [completing, setCompleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === createdById || me.role === "Moderator" || me.role === "Admin");
  if (!canManage) return null;

  const canComplete = (status === "Scheduled" || status === "Confirmed") && new Date(endsAt) <= new Date();

  async function handleDelete() {
    if (!window.confirm(t("deleteConfirm"))) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteEvent(apiUrl, locale, eventId);
      router.push("/events");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("deleteError"));
      setDeleting(false);
    }
  }

  async function handleComplete() {
    setCompleting(true);
    setError(null);
    try {
      await completeEvent(apiUrl, locale, eventId);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("completeError"));
    } finally {
      setCompleting(false);
    }
  }

  return (
    <div className="mt-4 flex flex-wrap items-center gap-3">
      {canComplete && (
        <button
          type="button"
          onClick={handleComplete}
          disabled={completing}
          className="cursor-pointer text-sm font-medium text-brand-primary transition-colors duration-200 hover:underline disabled:opacity-50"
        >
          {completing ? t("completing") : t("completeEvent")}
        </button>
      )}
      <button
        type="button"
        onClick={handleDelete}
        disabled={deleting}
        className="cursor-pointer text-sm font-medium text-red-600 transition-colors duration-200 hover:underline disabled:opacity-50 dark:text-red-400"
      >
        {deleting ? t("deleting") : t("delete")}
      </button>
      {error && <p className="w-full text-sm text-red-600">{error}</p>}
    </div>
  );
}
