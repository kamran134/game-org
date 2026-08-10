"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import {
  getNotificationPreferences,
  setNotificationPreference,
  type NotificationPreference as Preference,
} from "@/lib/notificationsApi";

export function NotificationPreferences({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Notifications");
  const tTypes = useTranslations("Notifications.types");

  const [prefs, setPrefs] = useState<Preference[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [savingType, setSavingType] = useState<string | null>(null);

  useEffect(() => {
    getNotificationPreferences(apiUrl)
      .then(setPrefs)
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, t]);

  async function handleToggle(type: Preference["type"], enabled: boolean) {
    setSavingType(type);
    setError(null);
    setPrefs((prev) => prev?.map((p) => (p.type === type ? { ...p, enabled } : p)) ?? null);
    try {
      await setNotificationPreference(apiUrl, type, enabled);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("saveError"));
      setPrefs((prev) => prev?.map((p) => (p.type === type ? { ...p, enabled: !enabled } : p)) ?? null);
    } finally {
      setSavingType(null);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/me" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {t("backToProfile")}
        </Link>
        <h1 className="font-heading mt-2 text-2xl font-semibold text-foreground">{t("preferencesTitle")}</h1>
        <p className="mt-1 text-sm text-foreground/60">{t("preferencesSubtitle")}</p>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {prefs === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {prefs.map((pref) => (
            <li
              key={pref.type}
              className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4"
            >
              <span className="text-foreground">{tTypes(pref.type)}</span>
              <label className="relative inline-flex cursor-pointer items-center">
                <input
                  type="checkbox"
                  checked={pref.enabled}
                  disabled={savingType === pref.type}
                  onChange={(e) => handleToggle(pref.type, e.target.checked)}
                  className="peer sr-only"
                />
                <span className="h-6 w-11 rounded-full bg-brand-muted transition-colors duration-200 peer-checked:bg-brand-primary peer-disabled:opacity-50" />
                <span className="absolute left-1 top-1 h-4 w-4 rounded-full bg-background transition-transform duration-200 peer-checked:translate-x-5" />
              </label>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
