import { getFormatter, getTranslations, setRequestLocale } from "next-intl/server";
import { CalendarBlank, MapPinLine, UsersThree } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getEvents } from "@/lib/eventsApi";

export const dynamic = "force-dynamic";

export default async function EventsPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ sportId?: string; past?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Events");
  const format = await getFormatter();
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

        <div className="mb-6 flex gap-2 text-sm font-medium">
          <Link
            href="/events"
            className={`rounded-full border px-4 py-1.5 transition-colors duration-200 ${
              upcoming
                ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
                : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
            }`}
          >
            {t("upcoming")}
          </Link>
          <Link
            href="/events?past=1"
            className={`rounded-full border px-4 py-1.5 transition-colors duration-200 ${
              !upcoming
                ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
                : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
            }`}
          >
            {t("past")}
          </Link>
        </div>

        {events.length === 0 ? (
          <p className="text-foreground/70">{t("empty")}</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {events.map((ev) => {
              const place = ev.venue?.name ?? ev.customLocation;
              const spotsLeft = ev.maxParticipants != null ? ev.maxParticipants - ev.confirmedCount : null;
              return (
                <li key={ev.id}>
                  <Link
                    href={`/events/${ev.publicId}`}
                    className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                  >
                    <div>
                      <p className="font-medium text-foreground">
                        {ev.sport.emoji} {ev.title ?? ev.sport.nameI18n[locale] ?? ev.sport.slug}
                      </p>
                      <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                        <CalendarBlank size={14} weight="bold" />
                        {format.dateTime(new Date(ev.startsAt), { dateStyle: "medium", timeStyle: "short" })}
                      </p>
                      {place && (
                        <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                          <MapPinLine size={14} weight="bold" />
                          {place}
                        </p>
                      )}
                    </div>
                    <div className="flex shrink-0 flex-col items-end gap-1 text-sm text-foreground/60">
                      {ev.status === "Cancelled" ? (
                        <span className="text-red-600">{t("eventCancelledNotice")}</span>
                      ) : (
                        <span className="flex items-center gap-1">
                          <UsersThree size={14} weight="bold" />
                          {spotsLeft != null ? (spotsLeft > 0 ? t("spotsLeft", { count: spotsLeft }) : t("full")) : ev.confirmedCount}
                        </span>
                      )}
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </main>
  );
}
