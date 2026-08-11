import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { getFollowers } from "@/lib/socialApi";
import { Link } from "@/i18n/navigation";

export const dynamic = "force-dynamic";

// Простой список без действий (не ClubMembersView) — управлять тут нечем,
// это чужие подписчики. targetId резолвим через тот же анонимный Kiota-запрос
// профиля, что и родительская /[handle] (additionalData.id — см. её комментарий).
export default async function FollowersPage({ params }: { params: Promise<{ locale: string; handle: string }> }) {
  const { locale, handle } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Social");
  const tProfile = await getTranslations("Profile");
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const client = createApiClient();
  const profile = await client.api.users.byHandle(handle).get().catch(() => null);
  const profileId = profile?.additionalData?.id as string | undefined;

  if (!profile || !profileId) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{tProfile("notFound")}.</p>
      </main>
    );
  }

  const followers = await getFollowers(apiUrl, locale, "User", profileId, { take: 50 });

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <Link href={`/${handle}`} className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {profile.displayName ?? handle}
        </Link>
        <h1 className="mt-2 font-heading text-2xl font-semibold text-foreground">{t("followersTitle")}</h1>

        {followers.length === 0 ? (
          <p className="mt-4 text-foreground/70">{t("noFollowers")}</p>
        ) : (
          <ul className="mt-4 flex flex-col gap-2">
            {followers.map((f) => (
              <li key={f.targetId}>
                <Link
                  href={`/${f.slug}`}
                  className="flex items-center gap-2 rounded-2xl border border-brand-border bg-background px-4 py-3 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <span className="font-medium text-foreground">{f.name}</span>
                  <span className="text-sm text-foreground/50">@{f.slug}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
