import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { MeEditor } from "./MeEditor";

export const dynamic = "force-dynamic";

export default async function MePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Me");

  // NEXT_PUBLIC_API_URL читается здесь (на сервере), не в клиентском
  // компоненте — см. комментарий в app/[locale]/login/page.tsx про build-time inlining.
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const client = createApiClient();
  const [cities, sports] = await Promise.all([client.api.cities.get(), client.api.sports.get()]);

  const cityOptions = (cities ?? []).map((c) => ({
    id: c.id!,
    name: (c.nameI18n?.additionalData?.[locale] as string | undefined) ?? c.slug!,
  }));

  const sportOptions = (sports ?? []).map((s) => ({
    id: s.id!,
    name: (s.nameI18n?.additionalData?.[locale] as string | undefined) ?? s.slug!,
    emoji: s.emoji ?? undefined,
  }));

  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="mb-6 text-2xl font-semibold">{t("pageTitle")}</h1>
      <MeEditor apiUrl={apiUrl} cities={cityOptions} sports={sportOptions} />
    </main>
  );
}
