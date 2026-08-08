import { getTranslations, setRequestLocale } from "next-intl/server";
import { TelegramLoginWidget } from "./TelegramLoginWidget";

export const dynamic = "force-dynamic";

export default async function LoginPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Login");

  // NEXT_PUBLIC_* читается здесь (на сервере), а не в клиентском компоненте —
  // apps/web/Dockerfile не передаёт build-time ARG для этих переменных, значит
  // клиентский инлайнинг на этапе `next build` подставил бы undefined. Читаем
  // на сервере в рантайме и передаём пропсами.
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  const botUsername = process.env.NEXT_PUBLIC_TELEGRAM_BOT_USERNAME ?? "";

  return (
    <main className="mx-auto flex max-w-md flex-col items-center gap-6 p-8">
      <h1 className="text-2xl font-semibold">{t("title")}</h1>
      {botUsername ? (
        <TelegramLoginWidget apiUrl={apiUrl} botUsername={botUsername} />
      ) : (
        <p className="text-sm text-red-600">{t("missingBotUsername")}</p>
      )}
    </main>
  );
}
