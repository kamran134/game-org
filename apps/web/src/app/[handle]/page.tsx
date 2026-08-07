import { cache } from "react";
import type { Metadata } from "next";
import { createApiClient } from "@/lib/apiClient";

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
  params: Promise<{ handle: string }>;
}): Promise<Metadata> {
  const { handle } = await params;
  const profile = await getProfile(handle);
  if (!profile) return { title: "Профиль не найден" };

  const title = profile.displayName ?? handle;
  const description = profile.bio || `Профиль игрока ${title} на game.org.az`;

  return {
    title,
    description,
    openGraph: { title, description, type: "profile" },
  };
}

export default async function PublicProfilePage({ params }: { params: Promise<{ handle: string }> }) {
  const { handle } = await params;
  const profile = await getProfile(handle);

  if (!profile) {
    return (
      <main className="mx-auto max-w-2xl p-8">
        <p>Профиль не найден.</p>
      </main>
    );
  }

  const cityName = profile.city?.nameI18n?.additionalData?.ru as string | undefined;

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
              <span className="ml-2 text-sm opacity-70">{s.level}</span>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
