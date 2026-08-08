import { getTranslations, setRequestLocale } from "next-intl/server";
import { MapPinLine, Star } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getVenues } from "@/lib/venuesApi";
import { NearMeButton } from "./NearMeButton";

export const dynamic = "force-dynamic";

export default async function VenuesPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ lat?: string; lng?: string; cityId?: string; sportId?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Venues");
  const sp = await searchParams;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const venues = await getVenues(apiUrl, {
    cityId: sp.cityId,
    sportId: sp.sportId,
    lat: sp.lat ? Number(sp.lat) : undefined,
    lng: sp.lng ? Number(sp.lng) : undefined,
  }).catch(() => []);

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>
          <div className="flex items-center gap-3">
            <NearMeButton />
            <Link
              href="/venues/new"
              className="inline-flex items-center rounded-full bg-brand-primary px-5 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
            >
              {t("addVenue")}
            </Link>
          </div>
        </div>

        {venues.length === 0 ? (
          <p className="text-foreground/70">{t("empty")}</p>
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
