"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getVenueAuthed, type VenueDetail } from "@/lib/venuesApi";
import { VenueForm } from "../../VenueForm";

type Option = { id: string; name: string };

// Тот же кредитный фолбэк, что DraftVenueGate у страницы просмотра — см.
// комментарий у getVenueAuthed в venuesApi.ts. Здесь нужен, потому что
// анонимный getVenue на сервере для своей же Draft-площадки вернёт 404 ещё
// до того, как VenueForm успеет сделать собственную проверку владения.
export function VenueEditGate({
  apiUrl,
  locale,
  slug,
  cities,
  sports,
}: {
  apiUrl: string;
  locale: string;
  slug: string;
  cities: Option[];
  sports: Option[];
}) {
  const t = useTranslations("Venues");
  const [venue, setVenue] = useState<VenueDetail | null | undefined>(undefined);

  useEffect(() => {
    getVenueAuthed(apiUrl, locale, slug)
      .then(setVenue)
      .catch(() => setVenue(null));
  }, [apiUrl, locale, slug]);

  if (venue === undefined) return null;
  if (!venue) return <p className="text-foreground/70">{t("notFound")}</p>;

  return <VenueForm apiUrl={apiUrl} cities={cities} sports={sports} mode="edit" venue={venue} />;
}
