import { redirect } from "@/i18n/navigation";

// Шаг 23: /moderation заменена панелью /admin — редирект сохраняет старые
// ссылки (закладки, уже отправленные уведомления и т.п.) рабочими.
export default async function ModerationRedirect({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  redirect({ href: "/admin", locale });
}
