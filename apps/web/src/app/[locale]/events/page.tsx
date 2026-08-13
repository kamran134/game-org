import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { createApiClient } from "@/lib/apiClient";
import { getEvents, type EventType } from "@/lib/eventsApi";
import { EventsList } from "./EventsList";

export const dynamic = "force-dynamic";

export default async function EventsPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{
    sportId?: string;
    cityId?: string;
    type?: string;
    onlyFree?: string;
    dateFrom?: string;
    dateTo?: string;
    past?: string;
    mine?: string;
    myClubs?: string;
  }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Events");
  const sp = await searchParams;
  const upcoming = sp.past !== "1";
  const type = sp.type === "Game" || sp.type === "Training" || sp.type === "Tournament" || sp.type === "Friendly" ? (sp.type as EventType) : undefined;
  const onlyFree = sp.onlyFree === "true" ? true : sp.onlyFree === "false" ? false : undefined;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const client = createApiClient();
  const [cities, sports, events] = await Promise.all([
    client.api.cities.get(),
    client.api.sports.get(),
    getEvents(apiUrl, locale, { sportId: sp.sportId, cityId: sp.cityId, upcoming, type, onlyFree, dateFrom: sp.dateFrom, dateTo: sp.dateTo }).catch(
      () => [],
    ),
  ]);

  const cityOptions = (cities ?? []).map((c) => ({
    id: c.id!,
    name: (c.nameI18n?.additionalData?.[locale] as string | undefined) ?? c.slug!,
  }));
  const sportOptions = (sports ?? []).map((s) => ({
    id: s.id!,
    name: (s.nameI18n?.additionalData?.[locale] as string | undefined) ?? s.slug!,
    emoji: s.emoji,
  }));

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>
          <Link
            href="/events/new"
            className="inline-flex items-center rounded-full bg-brand-primary px-5 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
          >
            {t("addEvent")}
          </Link>
        </div>

        <EventsList
          apiUrl={apiUrl}
          sportId={sp.sportId}
          cityId={sp.cityId}
          type={type}
          onlyFree={onlyFree}
          dateFrom={sp.dateFrom}
          dateTo={sp.dateTo}
          upcoming={upcoming}
          onlyMine={sp.mine === "1"}
          onlyMyClubs={sp.myClubs === "1"}
          query={sp}
          initialEvents={events}
          sports={sportOptions}
          cities={cityOptions}
        />
      </div>
    </main>
  );
}
