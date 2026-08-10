import { setRequestLocale } from "next-intl/server";
import { NotificationsList } from "./NotificationsList";

export const dynamic = "force-dynamic";

export default async function NotificationsPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-2xl">
        <NotificationsList apiUrl={apiUrl} />
      </div>
    </main>
  );
}
