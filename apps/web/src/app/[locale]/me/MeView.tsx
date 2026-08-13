"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getAdminOverview } from "@/lib/adminApi";

export function MeView({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Me");
  const tNav = useTranslations("Nav");
  const tLevels = useTranslations("Me.levels");
  const tVisibility = useTranslations("Me.visibilityOptions");
  const locale = useLocale();
  const router = useRouter();

  const [profile, setProfile] = useState<MeProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [moderationCount, setModerationCount] = useState<number | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then((p) => {
        if (!p) {
          router.push("/login");
          return;
        }
        setProfile(p);
      })
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")))
      .finally(() => setLoading(false));
  }, [apiUrl, locale, router, t]);

  const isModerator = profile?.role === "Moderator" || profile?.role === "Admin";

  useEffect(() => {
    if (!isModerator) return;
    getAdminOverview(apiUrl)
      .then((o) => setModerationCount(o.pendingVenues + o.pendingReports + o.pendingClaims))
      .catch(() => setModerationCount(null));
  }, [apiUrl, isModerator]);

  if (loading) return <p>{t("loading")}</p>;
  if (!profile) return null;
  if (error) return <p className="text-sm text-red-600">{error}</p>;

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-2xl border border-brand-border bg-background p-8">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="font-heading text-2xl font-semibold text-foreground">{profile.displayName}</h1>
            <p className="mt-1 text-sm text-brand-muted-foreground">@{profile.handle}</p>
            <p className="mt-2 text-sm text-foreground/60">{t("reliabilityScore", { score: profile.reliabilityScore })}</p>
          </div>
          <Link
            href="/me/edit"
            className="cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90"
          >
            {t("editProfile")}
          </Link>
        </div>

        {profile.bio && <p className="mt-4 text-foreground/80">{profile.bio}</p>}

        <dl className="mt-4 flex flex-wrap gap-x-6 gap-y-1 text-sm text-foreground/60">
          {profile.city && (
            <div className="flex gap-1.5">
              <dt className="font-medium text-foreground/70">{t("fields.city")}:</dt>
              <dd>{profile.city.nameI18n[locale] ?? profile.city.slug}</dd>
            </div>
          )}
          <div className="flex gap-1.5">
            <dt className="font-medium text-foreground/70">{t("fields.visibility")}:</dt>
            <dd>{tVisibility(profile.profileVisibility)}</dd>
          </div>
        </dl>

        <div className="mt-4 flex flex-wrap gap-4">
          <Link href="/me/notifications" className="text-sm font-medium text-brand-primary hover:underline">
            {t("notificationSettings")}
          </Link>
          <Link href="/me/payments" className="text-sm font-medium text-brand-primary hover:underline">
            {t("myPayments")}
          </Link>
        </div>
      </div>

      {isModerator && (
        <Link
          href="/admin"
          className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-6 py-4 transition-colors duration-200 hover:border-brand-primary/40"
        >
          <span className="font-medium text-foreground">{tNav("admin")}</span>
          {moderationCount !== null && moderationCount > 0 && (
            <span className="flex h-6 min-w-6 items-center justify-center rounded-full bg-brand-primary px-2 text-xs font-semibold text-brand-primary-foreground">
              {moderationCount}
            </span>
          )}
        </Link>
      )}

      <div className="flex flex-col gap-3">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("mySportsHeading")}</h2>

        {profile.sports.length === 0 && <p className="text-sm text-foreground/60">{t("noSports")}</p>}

        {profile.sports.length > 0 && (
          <ul className="flex flex-col gap-2">
            {profile.sports.map((s) => (
              <li
                key={s.sportId}
                className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4"
              >
                <span className="flex items-center gap-2 font-medium text-foreground">
                  <span className="text-lg">{s.sportEmoji}</span>
                  {s.sportSlug}
                </span>
                <span className="flex items-center gap-3 text-sm text-brand-muted-foreground">
                  {s.gamesPlayed > 0 && (
                    <span>
                      {t("rating", { rating: Math.round(s.rating) })} · {t("record", { wins: s.wins, draws: s.draws, losses: s.losses })}
                    </span>
                  )}
                  {tLevels(s.level)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h2 className="font-heading text-lg font-semibold text-foreground">{t("achievementsHeading")}</h2>
          <Link href="/me/achievements" className="text-sm font-medium text-brand-primary hover:underline">
            {t("viewAllAchievements")}
          </Link>
        </div>

        {profile.achievements.length === 0 ? (
          <p className="text-sm text-foreground/60">{t("noAchievements")}</p>
        ) : (
          <ul className="flex flex-wrap gap-3">
            {profile.achievements.map((a) => (
              <li
                key={a.code}
                title={a.description ?? undefined}
                className="flex items-center gap-2 rounded-2xl border border-brand-border bg-background px-4 py-2"
              >
                <span className="text-lg">{a.icon}</span>
                <span className="text-sm font-medium text-foreground">{a.name}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
