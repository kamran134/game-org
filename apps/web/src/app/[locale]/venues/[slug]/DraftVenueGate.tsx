"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { MapPinLine } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getVenueAuthed, type VenueDetail } from "@/lib/venuesApi";
import { VenueActions } from "./VenueActions";

// Рендерится, только когда анонимный SSR-фетч площадки вернул 404 — то есть
// либо площадки правда нет, либо это чья-то Draft-площадка, которую видит
// только автор/модератор (см. комментарий у getVenueAuthed в venuesApi.ts).
// Полную карточку с отзывами/фото/schema.org здесь не воспроизводим — это
// переходное состояние до публикации, не публичная страница для SEO.
export function DraftVenueGate({ apiUrl, locale, slug }: { apiUrl: string; locale: string; slug: string }) {
  const t = useTranslations("Venues");
  const [venue, setVenue] = useState<VenueDetail | null | undefined>(undefined);

  useEffect(() => {
    getVenueAuthed(apiUrl, locale, slug)
      .then(setVenue)
      .catch(() => setVenue(null));
  }, [apiUrl, locale, slug]);

  if (venue === undefined) return null;

  if (!venue) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto flex max-w-3xl flex-col gap-6">
        <Link href="/venues" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
          ← {t("backToList")}
        </Link>

        <div className="rounded-2xl border border-brand-border bg-background p-8">
          <h1 className="font-heading text-3xl font-semibold text-foreground">{venue.name}</h1>
          {venue.address && (
            <p className="mt-2 flex items-center gap-1.5 text-sm text-foreground/60">
              <MapPinLine size={16} weight="bold" />
              {venue.address}
            </p>
          )}

          <p className="mt-4 text-sm text-foreground/70">{t("draftNotice")}</p>

          <VenueActions apiUrl={apiUrl} venueId={venue.id} createdById={venue.createdById} status={venue.status} />

          <Link
            href={`/venues/${venue.slug}/edit`}
            className="mt-6 inline-block text-sm font-medium text-brand-primary hover:underline"
          >
            {t("editVenue")}
          </Link>
        </div>
      </div>
    </main>
  );
}
