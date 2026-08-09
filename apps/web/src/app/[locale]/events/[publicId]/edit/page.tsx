import { getTranslations, setRequestLocale } from "next-intl/server";
import { createApiClient } from "@/lib/apiClient";
import { getEvent } from "@/lib/eventsApi";
import { getVenues } from "@/lib/venuesApi";
import { EventForm } from "../../EventForm";

export const dynamic = "force-dynamic";

export default async function EditEventPage({
  params,
}: {
  params: Promise<{ locale: string; publicId: string }>;
}) {
  const { locale, publicId } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Events");

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const event = await getEvent(apiUrl, locale, publicId);

  if (!event) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

  const client = createApiClient();
  const [sports, venues] = await Promise.all([
    client.api.sports.get(),
    getVenues(apiUrl, locale, {}).catch(() => []),
  ]);

  const sportOptions = (sports ?? []).map((s) => ({
    id: s.id!,
    name: (s.nameI18n?.additionalData?.[locale] as string | undefined) ?? s.slug!,
  }));
  const venueOptions = venues.map((v) => ({ id: v.id, name: v.name }));

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <h1 className="font-heading mb-6 text-2xl font-semibold text-foreground">{t("editEvent")}</h1>
        <EventForm apiUrl={apiUrl} sports={sportOptions} venues={venueOptions} mode="edit" event={event} />
      </div>
    </main>
  );
}
