import { cache } from "react";
import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { MapPinLine, Phone, Globe } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getVenue, getVenueReviews } from "@/lib/venuesApi";
import { ReviewsSection } from "./ReviewsSection";
import { PhotoGallery } from "./PhotoGallery";
import { VenueActions } from "./VenueActions";
import { DraftVenueGate } from "./DraftVenueGate";
import { ReportButton } from "@/components/ReportButton";

export const dynamic = "force-dynamic";

const getVenueCached = cache(async (apiUrl: string, locale: string, slug: string) => getVenue(apiUrl, locale, slug));

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}): Promise<Metadata> {
  const { locale, slug } = await params;
  const t = await getTranslations({ locale, namespace: "Venues" });
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const venue = await getVenueCached(apiUrl, locale, slug);
  if (!venue) return { title: t("notFound") };

  const description = venue.description ?? venue.address ?? venue.name;
  return {
    title: venue.name,
    description,
    openGraph: {
      title: venue.name,
      description,
      type: "website",
      images: venue.photos[0] ? [venue.photos[0].url] : undefined,
    },
  };
}

export default async function VenuePage({ params }: { params: Promise<{ locale: string; slug: string }> }) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Venues");
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const venue = await getVenueCached(apiUrl, locale, slug);

  if (!venue) {
    return <DraftVenueGate apiUrl={apiUrl} locale={locale} slug={slug} />;
  }

  const reviews = await getVenueReviews(apiUrl, locale, venue.id).catch(() => []);

  // schema.org — для SEO (SportsActivityLocation: имя, адрес, координаты,
  // средний рейтинг, обложка). Тот же приём с инлайн-скриптом, что и
  // THEME_INIT_SCRIPT в layout, просто JSON вместо кода.
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "SportsActivityLocation",
    name: venue.name,
    description: venue.description ?? undefined,
    address: venue.address ?? undefined,
    geo: { "@type": "GeoCoordinates", latitude: venue.lat, longitude: venue.lng },
    telephone: venue.phone ?? undefined,
    url: venue.website ?? undefined,
    image: venue.photos[0]?.url,
    aggregateRating:
      venue.ratingCount > 0
        ? { "@type": "AggregateRating", ratingValue: venue.ratingAvg, reviewCount: venue.ratingCount }
        : undefined,
  };

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
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
          {venue.description && <p className="mt-4 text-foreground/80">{venue.description}</p>}

          {venue.sports.length > 0 && (
            <div className="mt-4 flex flex-wrap gap-2">
              {venue.sports.map((s) => (
                <span
                  key={s.sport.id}
                  className="rounded-full bg-brand-primary/10 px-3 py-1 text-sm font-medium text-brand-primary"
                >
                  {s.sport.emoji} {s.sport.nameI18n[locale] ?? s.sport.slug}
                </span>
              ))}
            </div>
          )}

          {(venue.hasLighting || venue.hasShowers || venue.hasParking || venue.hasTribunes) && (
            <div className="mt-4 flex flex-wrap gap-2 text-sm">
              {venue.hasLighting && <span className="rounded-full bg-brand-muted px-3 py-1 text-brand-foreground">{t("fields.hasLighting")}</span>}
              {venue.hasShowers && <span className="rounded-full bg-brand-muted px-3 py-1 text-brand-foreground">{t("fields.hasShowers")}</span>}
              {venue.hasParking && <span className="rounded-full bg-brand-muted px-3 py-1 text-brand-foreground">{t("fields.hasParking")}</span>}
              {venue.hasTribunes && <span className="rounded-full bg-brand-muted px-3 py-1 text-brand-foreground">{t("fields.hasTribunes")}</span>}
            </div>
          )}

          <div className="mt-6 flex flex-wrap gap-4 text-sm text-foreground/70">
            {venue.priceHint != null && (
              <span>
                {venue.priceHint} {venue.currency}
                {t("perHour")}
              </span>
            )}
            {venue.phone && (
              <a href={`tel:${venue.phone}`} className="flex items-center gap-1 transition-colors duration-200 hover:text-foreground">
                <Phone size={14} weight="bold" /> {venue.phone}
              </a>
            )}
            {venue.website && (
              <a
                href={venue.website}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 transition-colors duration-200 hover:text-foreground"
              >
                <Globe size={14} weight="bold" /> {venue.website}
              </a>
            )}
          </div>

          <VenueActions apiUrl={apiUrl} venueId={venue.id} createdById={venue.createdById} status={venue.status} />

          <div className="mt-6 flex flex-wrap items-center gap-4">
            <Link href={`/venues/${venue.slug}/edit`} className="text-sm font-medium text-brand-primary hover:underline">
              {t("editVenue")}
            </Link>
            <ReportButton apiUrl={apiUrl} targetType="Venue" targetId={venue.id} />
          </div>
        </div>

        <PhotoGallery apiUrl={apiUrl} locale={locale} venueId={venue.id} initialPhotos={venue.photos} />
        <ReviewsSection apiUrl={apiUrl} locale={locale} venueId={venue.id} initialReviews={reviews} />
      </div>
    </main>
  );
}
