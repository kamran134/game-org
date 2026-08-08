import { cache } from "react";
import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { MapPinLine } from "@phosphor-icons/react/dist/ssr";
import { createApiClient } from "@/lib/apiClient";
import { routing } from "@/i18n/routing";
import { getPathname } from "@/i18n/navigation";

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
        </div>

        {profile.sports && profile.sports.length > 0 && (
          <ul className="mt-6 flex flex-col gap-3">
            {profile.sports.map((s, i) => (
              <li
                key={i}
                className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
              >
                <span className="flex items-center gap-2 font-medium text-foreground">
                  <span className="text-lg">{s.sportEmoji}</span>
                  {s.sportSlug}
                </span>
                <span className="text-sm text-brand-muted-foreground">{s.level ? tLevels(s.level) : null}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
