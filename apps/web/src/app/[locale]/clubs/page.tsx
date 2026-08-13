import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { createApiClient } from "@/lib/apiClient";
import { getClubs } from "@/lib/clubsApi";
import { ClubsList } from "./ClubsList";

export const dynamic = "force-dynamic";

export default async function ClubsPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ cityId?: string; sportId?: string; mine?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Clubs");
  const sp = await searchParams;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const client = createApiClient();
  const [cities, sports, clubs] = await Promise.all([
    client.api.cities.get(),
    client.api.sports.get(),
    getClubs(apiUrl, locale, { cityId: sp.cityId, sportId: sp.sportId, kind: "Club" }).catch(() => []),
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
            href="/clubs/new"
            className="inline-flex items-center rounded-full bg-brand-primary px-5 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
          >
            {t("addClub")}
          </Link>
        </div>

        <ClubsList
          apiUrl={apiUrl}
          basePath="/clubs"
          cityId={sp.cityId}
          sportId={sp.sportId}
          kind="Club"
          onlyMine={sp.mine === "1"}
          query={sp}
          initialClubs={clubs}
          sports={sportOptions}
          cities={cityOptions}
        />
      </div>
    </main>
  );
}
