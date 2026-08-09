"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { deleteEvent } from "@/lib/eventsApi";

export function EventActions({ apiUrl, eventId, createdById }: { apiUrl: string; eventId: string; createdById?: string | null }) {
  const t = useTranslations("Events");
  const locale = useLocale();
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === createdById || me.role === "Moderator" || me.role === "Admin");
  if (!canManage) return null;

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

  return (
    <div className="mt-4 flex flex-wrap items-center gap-3">
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
