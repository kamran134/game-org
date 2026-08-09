import { setRequestLocale } from "next-intl/server";
import { ModerationQueue } from "./ModerationQueue";

export const dynamic = "force-dynamic";

export default async function ModerationPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <ModerationQueue apiUrl={apiUrl} />
      </div>
    </main>
  );
}
