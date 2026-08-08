import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";

export const dynamic = "force-dynamic";

export default async function SportsPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Sports");

  const client = createApiClient();
  const sports = (await client.api.sports.get()) ?? [];

  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="mb-6 text-2xl font-semibold">{t("pageTitle")}</h1>
      <ul className="flex flex-col gap-2">
        {sports.map((sport) => (
          <li
            key={sport.id}
            className="flex items-center gap-3 rounded-lg border border-black/10 px-4 py-3 dark:border-white/10"
          >
            <span className="text-xl">{sport.emoji}</span>
            {/*
              nameI18n — открытый словарь (Dictionary<string,string> в C#, "additionalProperties"
              в OpenAPI), у него нет фиксированных полей в схеме — Kiota кладёт значения
              в additionalData, а не как обычные именованные свойства.
            */}
            <span>{(sport.nameI18n?.additionalData?.[locale] as string | undefined) ?? sport.slug}</span>
          </li>
        ))}
      </ul>
    </main>
  );
}
