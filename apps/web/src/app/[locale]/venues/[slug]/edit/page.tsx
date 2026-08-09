import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { getVenue } from "@/lib/venuesApi";
import { VenueForm } from "../../VenueForm";

export const dynamic = "force-dynamic";

export default async function EditVenuePage({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Venues");

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const venue = await getVenue(apiUrl, locale, slug);

  if (!venue) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

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
        <h1 className="font-heading mb-6 text-2xl font-semibold text-foreground">{t("editVenue")}</h1>
        <VenueForm apiUrl={apiUrl} cities={cityOptions} sports={sportOptions} mode="edit" venue={venue} />
      </div>
    </main>
  );
}
