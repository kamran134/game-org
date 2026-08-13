import { setRequestLocale, getTranslations } from "next-intl/server";
import { VenuesQueue } from "../VenuesQueue";

export const dynamic = "force-dynamic";

export default async function AdminVenuesPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Admin");
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <div className="flex flex-col gap-6">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("sidebar.venues")}</h1>
      <VenuesQueue apiUrl={apiUrl} />
    </div>
  );
}
