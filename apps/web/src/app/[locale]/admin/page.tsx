import { setRequestLocale } from "next-intl/server";
import { OverviewSection } from "./OverviewSection";

export const dynamic = "force-dynamic";

export default async function AdminOverviewPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return <OverviewSection apiUrl={apiUrl} />;
}
