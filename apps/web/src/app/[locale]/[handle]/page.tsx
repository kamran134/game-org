import { cache } from "react";
import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
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
      <main className="mx-auto max-w-2xl p-8">
        <p>{t("notFound")}.</p>
      </main>
    );
  }

  const cityName = profile.city?.nameI18n?.additionalData?.[locale] as string | undefined;

  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="text-2xl font-semibold">{profile.displayName}</h1>
      <p className="text-sm opacity-70">@{profile.handle}</p>
      {profile.bio && <p className="mt-4">{profile.bio}</p>}
      {profile.city && <p className="mt-2 text-sm opacity-70">{cityName ?? profile.city.slug}</p>}

      {profile.sports && profile.sports.length > 0 && (
        <ul className="mt-6 flex flex-col gap-2">
          {profile.sports.map((s, i) => (
            <li key={i} className="rounded-lg border border-black/10 px-4 py-3 dark:border-white/10">
              <span>
                {s.sportEmoji} {s.sportSlug}
              </span>
              <span className="ml-2 text-sm opacity-70">{s.level ? tLevels(s.level) : null}</span>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
