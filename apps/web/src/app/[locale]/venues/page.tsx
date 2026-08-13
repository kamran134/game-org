import { getTranslations, setRequestLocale } from "next-intl/server";
import { MapPinLine, Star } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { createApiClient } from "@/lib/apiClient";
import { getVenues } from "@/lib/venuesApi";
import { VenuesFilterBar } from "./VenuesFilterBar";

export const dynamic = "force-dynamic";

export default async function VenuesPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ lat?: string; lng?: string; cityId?: string; sportId?: string; isIndoor?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Venues");
  const sp = await searchParams;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const isIndoor = sp.isIndoor === "true" ? true : sp.isIndoor === "false" ? false : undefined;

  const client = createApiClient();
  const [cities, sports, venues] = await Promise.all([
    client.api.cities.get(),
    client.api.sports.get(),
    getVenues(apiUrl, locale, {
      cityId: sp.cityId,
      sportId: sp.sportId,
      isIndoor,
      lat: sp.lat ? Number(sp.lat) : undefined,
      lng: sp.lng ? Number(sp.lng) : undefined,
    }).catch(() => []),
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

  const hasFilters = !!(sp.cityId || sp.sportId || sp.isIndoor || sp.lat);

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>
          <Link
            href="/venues/new"
            className="inline-flex items-center rounded-full bg-brand-primary px-5 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
          >
            {t("addVenue")}
          </Link>
        </div>

        <VenuesFilterBar sportId={sp.sportId} cityId={sp.cityId} isIndoor={sp.isIndoor} query={sp} sports={sportOptions} cities={cityOptions} />

        {venues.length === 0 ? (
          hasFilters ? (
            <div className="flex flex-col items-start gap-2">
              <p className="text-foreground/70">{t("emptyFiltered")}</p>
              <Link href="/venues" className="text-sm font-medium text-brand-primary hover:underline">
                {t("resetFilters")}
              </Link>
            </div>
          ) : (
            <p className="text-foreground/70">{t("empty")}</p>
          )
        ) : (
          <ul className="flex flex-col gap-3">
            {venues.map((v) => (
              <li key={v.id}>
                <Link
                  href={`/venues/${v.slug}`}
                  className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <div>
                    <p className="font-medium text-foreground">{v.name}</p>
                    {v.address && (
                      <p className="mt-1 flex items-center gap-1 text-sm text-foreground/60">
                        <MapPinLine size={14} weight="bold" />
                        {v.address}
                      </p>
                    )}
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1 text-sm">
                    {v.ratingCount > 0 && (
                      <span className="flex items-center gap-1 text-brand-accent">
                        <Star size={14} weight="fill" />
                        {v.ratingAvg?.toFixed(1)}
                      </span>
                    )}
                    {v.distanceMeters != null && (
                      <span className="text-foreground/50">
                        {t("distanceKm", { km: (v.distanceMeters / 1000).toFixed(1) })}
                      </span>
                    )}
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
