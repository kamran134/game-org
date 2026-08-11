import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { Link } from "@/i18n/navigation";

export const dynamic = "force-dynamic";

export default async function SportsPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Sports");

  const client = createApiClient();
  const sports = (await client.api.sports.get()) ?? [];

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <h1 className="font-heading mb-6 text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>
        <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {sports.map((sport) => (
            <li
              key={sport.id}
              className="flex items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-4 py-3 transition-colors duration-200 hover:border-brand-primary/40"
            >
              <span className="flex items-center gap-3">
                <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-primary/10 text-xl">
                  {sport.emoji}
                </span>
                {/*
                  nameI18n — открытый словарь (Dictionary<string,string> в C#, "additionalProperties"
                  в OpenAPI), у него нет фиксированных полей в схеме — Kiota кладёт значения
                  в additionalData, а не как обычные именованные свойства.
                */}
                <span className="font-medium text-foreground">
                  {(sport.nameI18n?.additionalData?.[locale] as string | undefined) ?? sport.slug}
                </span>
              </span>
              {sport.slug && (
                <Link href={`/sports/${sport.slug}/leaderboard`} className="shrink-0 text-sm font-medium text-brand-primary hover:underline">
                  {t("leaderboard")}
                </Link>
              )}
            </li>
          ))}
        </ul>
      </div>
    </main>
  );
}
