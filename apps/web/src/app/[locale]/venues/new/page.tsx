import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { VenueForm } from "../VenueForm";

export const dynamic = "force-dynamic";

export default async function NewVenuePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Venues");

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
  }));

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <h1 className="font-heading mb-6 text-2xl font-semibold text-foreground">{t("addVenue")}</h1>
        <VenueForm apiUrl={apiUrl} cities={cityOptions} sports={sportOptions} mode="create" />
      </div>
    </main>
  );
}
