import { cache } from "react";
import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getClub } from "@/lib/clubsApi";
import { ClubView } from "./ClubView";

export const dynamic = "force-dynamic";

// Анонимный — Private/чужие клубы вернут null здесь даже участнику
// (viewerId неоткуда взять на сервере), но для generateMetadata это
// достаточно: в худшем случае заголовок будет дефолтным, тело страницы
// (ClubView) всё равно дозапросит клуб на клиенте с cookie.
const getClubCached = cache(async (apiUrl: string, locale: string, slug: string) => getClub(apiUrl, locale, slug));

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}): Promise<Metadata> {
  const { locale, slug } = await params;
  const t = await getTranslations({ locale, namespace: "Clubs" });
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const club = await getClubCached(apiUrl, locale, slug);
  if (!club) return { title: t("pageTitle") };

  return { title: club.name, description: club.description ?? undefined };
}

export default async function ClubPage({ params }: { params: Promise<{ locale: string; slug: string }> }) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return <ClubView apiUrl={apiUrl} locale={locale} slug={slug} />;
}
