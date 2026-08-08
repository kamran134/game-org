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
    <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
      <div className="flex w-full max-w-sm flex-col items-center gap-6 rounded-2xl border border-brand-border bg-background p-8 shadow-sm">
        <h1 className="font-heading text-2xl font-semibold text-foreground">{t("title")}</h1>
        {botUsername ? (
          <TelegramLoginWidget apiUrl={apiUrl} botUsername={botUsername} />
        ) : (
          <p className="text-sm text-red-600">{t("missingBotUsername")}</p>
        )}
      </div>
    </main>
  );
}
