import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { getFollowing, type FollowSummary } from "@/lib/socialApi";
import { Link } from "@/i18n/navigation";

export const dynamic = "force-dynamic";

function targetHref(f: FollowSummary): string {
  if (f.targetType === "Club") return `/clubs/${f.slug}`;
  if (f.targetType === "Venue") return `/venues/${f.slug}`;
  return `/${f.slug}`;
}

export default async function FollowingPage({ params }: { params: Promise<{ locale: string; handle: string }> }) {
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

  const following = await getFollowing(apiUrl, locale, profileId, { take: 50 });

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <Link href={`/${handle}`} className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {profile.displayName ?? handle}
        </Link>
        <h1 className="mt-2 font-heading text-2xl font-semibold text-foreground">{t("followingTitle")}</h1>

        {following.length === 0 ? (
          <p className="mt-4 text-foreground/70">{t("noFollowing")}</p>
        ) : (
          <ul className="mt-4 flex flex-col gap-2">
            {following.map((f) => (
              <li key={`${f.targetType}:${f.targetId}`}>
                <Link
                  href={targetHref(f)}
                  className="flex items-center gap-2 rounded-2xl border border-brand-border bg-background px-4 py-3 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <span className="font-medium text-foreground">{f.name}</span>
                  <span className="text-sm text-foreground/50">{t(`targetType.${f.targetType}`)}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
