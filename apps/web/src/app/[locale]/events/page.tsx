import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { getEvents } from "@/lib/eventsApi";
import { EventsList } from "./EventsList";

export const dynamic = "force-dynamic";

export default async function EventsPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ sportId?: string; past?: string; mine?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Events");
  const sp = await searchParams;
  const upcoming = sp.past !== "1";

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const events = await getEvents(apiUrl, locale, { sportId: sp.sportId, upcoming }).catch(() => []);

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

        <EventsList apiUrl={apiUrl} sportId={sp.sportId} upcoming={upcoming} initialEvents={events} />
      </div>
    </main>
  );
}
