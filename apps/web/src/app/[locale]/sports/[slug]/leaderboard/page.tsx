import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { getLeaderboard } from "@/lib/reputationApi";
import { Link } from "@/i18n/navigation";

export const dynamic = "force-dynamic";

export default async function SportLeaderboardPage({ params }: { params: Promise<{ locale: string; slug: string }> }) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Sports");
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const client = createApiClient();
  const sports = (await client.api.sports.get()) ?? [];
  const sport = sports.find((s) => s.slug === slug);

  if (!sport) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

  const sportName = (sport.nameI18n?.additionalData?.[locale] as string | undefined) ?? sport.slug ?? slug;
  const entries = await getLeaderboard(apiUrl, locale, slug).catch(() => []);

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <Link href="/sports" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {t("pageTitle")}
        </Link>

        <h1 className="mt-2 font-heading text-2xl font-semibold text-foreground">
          {sport.emoji} {t("leaderboardHeading", { sport: sportName })}
        </h1>

        {entries.length === 0 ? (
          <p className="mt-4 text-foreground/70">{t("leaderboardEmpty")}</p>
        ) : (
          <ol className="mt-4 flex flex-col gap-2">
            {entries.map((entry, i) => (
              <li
                key={entry.userId}
                className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4"
              >
                <span className="flex items-center gap-3">
                  <span className="w-6 text-right text-sm font-semibold text-foreground/50">{i + 1}</span>
                  <Link href={`/${entry.handle}`} className="font-medium text-foreground hover:underline">
                    {entry.displayName}
                  </Link>
                </span>
                <span className="flex items-center gap-3 text-sm text-brand-muted-foreground">
                  <span>{t("record", { wins: entry.wins, draws: entry.draws, losses: entry.losses })}</span>
                  <span className="font-semibold text-foreground">{Math.round(entry.rating)}</span>
                </span>
              </li>
            ))}
          </ol>
        )}
      </div>
    </main>
  );
}
