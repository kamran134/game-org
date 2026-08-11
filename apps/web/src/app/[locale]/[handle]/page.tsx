import { cache } from "react";
import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { MapPinLine } from "@phosphor-icons/react/dist/ssr";
import { createApiClient } from "@/lib/apiClient";
import { routing } from "@/i18n/routing";
import { Link, getPathname } from "@/i18n/navigation";
import { ReportButton } from "@/components/ReportButton";
import { FollowButton } from "@/components/FollowButton";

export const dynamic = "force-dynamic";

// params.handle приходит уже раскодированным Next.js, "@" внутри значения —
// обычный символ. Бэкенд сам обрезает ведущий "@" (см. ProfileEndpoints.cs),
// сюда передаём как есть.
const getProfile = cache(async (handle: string) => {
  const client = createApiClient();
  try {
    return await client.api.users.byHandle(handle).get();
  } catch {
    return null;
  }
});

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; handle: string }>;
}): Promise<Metadata> {
  const { locale, handle } = await params;
  const t = await getTranslations({ locale, namespace: "Profile" });
  const profile = await getProfile(handle);
  if (!profile) return { title: t("notFound") };

  const title = profile.displayName ?? handle;
  const description = profile.bio || t("descriptionFallback", { name: title });

  const languages = Object.fromEntries(
    routing.locales.map((l) => [l, getPathname({ locale: l, href: `/${handle}` })]),
  );

  return {
    title,
    description,
    openGraph: { title, description, type: "profile" },
    alternates: { languages },
  };
}

export default async function PublicProfilePage({
  params,
}: {
  params: Promise<{ locale: string; handle: string }>;
}) {
  const { locale, handle } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Profile");
  const tLevels = await getTranslations("Me.levels");
  const profile = await getProfile(handle);

  if (!profile) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}.</p>
      </main>
    );
  }

  const cityName = profile.city?.nameI18n?.additionalData?.[locale] as string | undefined;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  // PublicProfileDto.Id/FollowersCount — новые поля, добавлены на бэкенде
  // позже, чем сгенерирован Kiota-клиент (требует живой API+БД, здесь
  // недоступно). Kiota кладёт нераспознанные top-level свойства в
  // additionalData вместо того, чтобы их терять — тот же механизм, что уже
  // используется для nameI18n. ViewerIsFollowing сюда не тащим — этот
  // анонимный SSR-запрос без cookie всегда возвращал бы false, реальное
  // состояние подписки FollowButton определяет сам на клиенте.
  const profileId = profile.additionalData?.id as string | undefined;
  const followersCount = profile.additionalData?.followersCount as number | undefined;
  const reliabilityScore = profile.additionalData?.reliabilityScore as number | undefined;

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <div className="rounded-2xl border border-brand-border bg-background p-8">
          <h1 className="font-heading text-3xl font-semibold text-foreground">{profile.displayName}</h1>
          <p className="mt-1 text-sm text-brand-muted-foreground">@{profile.handle}</p>
          {profile.bio && <p className="mt-4 text-foreground/80">{profile.bio}</p>}
          {profile.city && (
            <p className="mt-3 flex items-center gap-1.5 text-sm text-foreground/60">
              <MapPinLine size={16} weight="bold" />
              {cityName ?? profile.city.slug}
            </p>
          )}
          {reliabilityScore != null && (
            <p className="mt-2 text-sm text-foreground/60">{t("reliabilityScore", { score: reliabilityScore })}</p>
          )}
          <p className="mt-3 flex items-center gap-3 text-sm text-foreground/60">
            {followersCount != null && (
              <Link href={`/${handle}/followers`} className="hover:text-foreground hover:underline">
                {t("followersCount", { count: followersCount })}
              </Link>
            )}
            <Link href={`/${handle}/following`} className="hover:text-foreground hover:underline">
              {t("followingLink")}
            </Link>
          </p>
          {profileId && (
            <div className="mt-4 flex flex-wrap items-center gap-4">
              <FollowButton apiUrl={apiUrl} targetType="User" targetId={profileId} />
              <ReportButton apiUrl={apiUrl} targetType="User" targetId={profileId} />
            </div>
          )}
        </div>

        {profile.sports && profile.sports.length > 0 && (
          <ul className="mt-6 flex flex-col gap-3">
            {profile.sports.map((s, i) => {
              // rating/gamesPlayed/wins/draws/losses — новые поля с Шага 15, тот же
              // additionalData-приём, что и для id/followersCount выше.
              const gamesPlayed = s.additionalData?.gamesPlayed as number | undefined;
              const rating = s.additionalData?.rating as number | undefined;
              const wins = s.additionalData?.wins as number | undefined;
              const draws = s.additionalData?.draws as number | undefined;
              const losses = s.additionalData?.losses as number | undefined;

              return (
                <li
                  key={i}
                  className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <span className="flex items-center gap-2 font-medium text-foreground">
                    <span className="text-lg">{s.sportEmoji}</span>
                    {s.sportSlug}
                  </span>
                  <span className="flex items-center gap-3 text-sm text-brand-muted-foreground">
                    {!!gamesPlayed && (
                      <span>
                        {t("rating", { rating: Math.round(rating ?? 1500) })} ·{" "}
                        {t("record", { wins: wins ?? 0, draws: draws ?? 0, losses: losses ?? 0 })}
                      </span>
                    )}
                    {s.level ? tLevels(s.level) : null}
                  </span>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </main>
  );
}
