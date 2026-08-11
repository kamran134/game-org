import { setRequestLocale } from "next-intl/server";
import { ClubMembersView } from "./ClubMembersView";

export const dynamic = "force-dynamic";

export default async function ClubMembersPage({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <ClubMembersView apiUrl={apiUrl} locale={locale} slug={slug} />
      </div>
    </main>
  );
}
