import { cache } from "react";
import type { Metadata } from "next";
import { getFormatter, getTranslations, setRequestLocale } from "next-intl/server";
import { CalendarBlank, MapPinLine, UsersThree } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getEvent } from "@/lib/eventsApi";
import { ParticipantsSection } from "./ParticipantsSection";
import { EventActions } from "./EventActions";
import { TeamsSection } from "./TeamsSection";
import { MvpVoteSection } from "./MvpVoteSection";
import { RecordResultSection } from "./RecordResultSection";
import { ReportButton } from "@/components/ReportButton";

export const dynamic = "force-dynamic";

const getEventCached = cache(async (apiUrl: string, locale: string, publicId: string) => getEvent(apiUrl, locale, publicId));

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; publicId: string }>;
}): Promise<Metadata> {
  const { locale, publicId } = await params;
  const t = await getTranslations({ locale, namespace: "Events" });
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const event = await getEventCached(apiUrl, locale, publicId);
  if (!event) return { title: t("notFound") };

  const title = event.title ?? `${event.sport.emoji ?? ""} ${event.sport.nameI18n[locale] ?? event.sport.slug}`.trim();
  const description = event.description ?? event.venue?.name ?? event.customLocation ?? title;
  return {
    title,
    description,
    openGraph: { title, description, type: "website" },
  };
}

export default async function EventPage({ params }: { params: Promise<{ locale: string; publicId: string }> }) {
  const { locale, publicId } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Events");
  const format = await getFormatter();
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const event = await getEventCached(apiUrl, locale, publicId);

  if (!event) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

  const place = event.venue?.name ?? event.customLocation;
  const title = event.title ?? `${event.sport.emoji ?? ""} ${event.sport.nameI18n[locale] ?? event.sport.slug}`.trim();

  // schema.org — тот же приём, что в /venues/[slug] (SportsActivityLocation).
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "SportsEvent",
    name: title,
    description: event.description ?? undefined,
    startDate: event.startsAt,
    endDate: event.endsAt,
    eventStatus: event.status === "Cancelled" ? "https://schema.org/EventCancelled" : "https://schema.org/EventScheduled",
    location: place ? { "@type": "Place", name: place } : undefined,
  };

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <div className="mx-auto flex max-w-3xl flex-col gap-6">
        <Link href="/events" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {t("backToList")}
        </Link>

        <div className="rounded-2xl border border-brand-border bg-background p-8">
          <h1 className="font-heading text-3xl font-semibold text-foreground">{title}</h1>

          <p className="mt-3 flex items-center gap-1.5 text-sm text-foreground/70">
            <CalendarBlank size={16} weight="bold" />
            {format.dateTime(new Date(event.startsAt), { dateStyle: "medium", timeStyle: "short" })}
            {" – "}
            {format.dateTime(new Date(event.endsAt), { timeStyle: "short" })}
          </p>

          {place && (
            <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/70">
              <MapPinLine size={16} weight="bold" />
              {event.venue ? (
                <Link href={`/venues/${event.venue.slug}`} className="hover:underline">
                  {place}
                </Link>
              ) : (
                place
              )}
            </p>
          )}

          <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/70">
            <UsersThree size={16} weight="bold" />
            {event.confirmedCount}
            {event.maxParticipants != null ? ` / ${event.maxParticipants}` : ""}
            {event.waitlistCount > 0 && ` · ${t("waitlistCount", { count: event.waitlistCount })}`}
          </p>

          {event.description && <p className="mt-4 text-foreground/80">{event.description}</p>}

          {event.costSplit !== "Free" && event.cost != null && (
            <p className="mt-4 text-sm text-foreground/70">
              {t(`costSplitOptions.${event.costSplit}`)}: {event.cost} {event.currency}
            </p>
          )}

          {event.result && (
            <div className="mt-4 rounded-xl border border-brand-border bg-brand-muted/40 p-4">
              <p className="text-sm font-medium text-foreground">{t("result.heading")}</p>
              {event.result.summary && <p className="mt-1 text-sm text-foreground/80">{event.result.summary}</p>}
              {event.result.mvpDisplayName && (
                <p className="mt-2 text-sm text-foreground/70">{t("mvp.badge", { name: event.result.mvpDisplayName })}</p>
              )}
            </div>
          )}

          <div className="mt-6 flex flex-wrap items-center gap-4">
            <Link href={`/events/${event.publicId}/edit`} className="text-sm font-medium text-brand-primary hover:underline">
              {t("editEvent")}
            </Link>
            <ReportButton apiUrl={apiUrl} targetType="Event" targetId={event.id} />
          </div>

          <EventActions apiUrl={apiUrl} eventId={event.id} createdById={event.createdById} status={event.status} endsAt={event.endsAt} />
        </div>

        <ParticipantsSection apiUrl={apiUrl} locale={locale} event={event} />
        <TeamsSection apiUrl={apiUrl} locale={locale} event={event} />
        <MvpVoteSection apiUrl={apiUrl} event={event} />
        <RecordResultSection apiUrl={apiUrl} locale={locale} event={event} />
      </div>
    </main>
  );
}
