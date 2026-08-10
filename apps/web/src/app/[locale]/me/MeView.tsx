"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getVenueModerationQueue, getVenueClaimQueue } from "@/lib/venuesApi";
import { getReportQueue } from "@/lib/moderationApi";

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
    Promise.all([
      getVenueModerationQueue(apiUrl, locale),
      getReportQueue(apiUrl, locale),
      getVenueClaimQueue(apiUrl, locale),
    ])
      .then(([venues, reports, claims]) => setModerationCount(venues.length + reports.length + claims.length))
      .catch(() => setModerationCount(null));
  }, [apiUrl, locale, isModerator]);

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
      </div>

      {isModerator && (
        <Link
          href="/moderation"
          className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-6 py-4 transition-colors duration-200 hover:border-brand-primary/40"
        >
          <span className="font-medium text-foreground">{tNav("moderation")}</span>
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
                <span className="text-sm text-brand-muted-foreground">{tLevels(s.level)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
