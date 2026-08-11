"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations, useFormatter } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getMyAchievements, type Achievement } from "@/lib/reputationApi";

export function AchievementsView({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Me");
  const locale = useLocale();
  const format = useFormatter();

  const [achievements, setAchievements] = useState<Achievement[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMyAchievements(apiUrl, locale)
      .then(setAchievements)
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, locale, t]);

  return (
    <div className="flex flex-col gap-6">
      <Link href="/me" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
        ← {t("pageTitle")}
      </Link>

      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("achievementsHeading")}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {achievements === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : (
        <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {achievements.map((a) => {
            const earned = a.earnedAt != null;
            return (
              <li
                key={a.code}
                className={`flex items-start gap-3 rounded-2xl border border-brand-border bg-background px-5 py-4 ${earned ? "" : "opacity-50"}`}
              >
                <span className="text-2xl">{a.icon}</span>
                <div>
                  <p className="font-medium text-foreground">{a.name}</p>
                  {a.description && <p className="mt-0.5 text-sm text-foreground/60">{a.description}</p>}
                  {earned && (
                    <p className="mt-1 text-xs text-brand-primary">
                      {t("earnedOn", { date: format.dateTime(new Date(a.earnedAt!), { dateStyle: "medium" }) })}
                    </p>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
