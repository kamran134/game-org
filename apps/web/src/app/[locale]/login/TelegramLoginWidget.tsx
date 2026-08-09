"use client";

import { useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, loginWithTelegram, type MeProfile } from "@/lib/authApi";

type TelegramWidgetUser = {
  id: number;
  first_name: string;
  last_name?: string;
  username?: string;
  photo_url?: string;
  auth_date: number;
  hash: string;
};

declare global {
  interface Window {
    onTelegramAuth?: (user: TelegramWidgetUser) => void;
  }
}

export function TelegramLoginWidget({ apiUrl, botUsername }: { apiUrl: string; botUsername: string }) {
  const t = useTranslations("Login");
  const locale = useLocale();
  const router = useRouter();
  const containerRef = useRef<HTMLDivElement>(null);
  const [checkingSession, setCheckingSession] = useState(true);
  const [authorizing, setAuthorizing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Уже залогинен — сразу на /me, виджет не нужен.
  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then((profile: MeProfile | null) => {
        if (profile) router.push("/me");
        else setCheckingSession(false);
      })
      .catch(() => setCheckingSession(false));
  }, [apiUrl, locale, router]);

  useEffect(() => {
    if (checkingSession || !containerRef.current) return;

    window.onTelegramAuth = (user) => {
      setAuthorizing(true);
      loginWithTelegram(apiUrl, locale, user)
        .then(() => router.push("/me"))
        .catch((err) => {
          setError(err instanceof Error ? err.message : t("error"));
          setAuthorizing(false);
        });
    };

    // Виджет сам вставляет себя (iframe-кнопку) на место этого <script> —
    // поэтому вставляем тег вручную в контейнер, а не через next/script
    // (тот может переместить скрипт в другое место DOM).
    const script = document.createElement("script");
    script.src = "https://telegram.org/js/telegram-widget.js?22";
    script.async = true;
    script.setAttribute("data-telegram-login", botUsername);
    script.setAttribute("data-size", "large");
    script.setAttribute("data-radius", "10");
    script.setAttribute("data-onauth", "onTelegramAuth(user)");
    script.setAttribute("data-request-access", "write");
    containerRef.current.appendChild(script);

    return () => {
      window.onTelegramAuth = undefined;
    };
  }, [apiUrl, botUsername, checkingSession, locale, router, t]);

  if (checkingSession) return null;

  return (
    <div className="flex flex-col items-center gap-3">
      {/* Виджет Telegram рендерит iframe с собственным (всегда светлым) фоном —
          нейтральная белая подложка не даёт ему смотреться посторонним пятном
          на тёмной теме сайта. Фиксированная высота — чтобы карточка не
          дёргалась, пока скрипт telegram.org грузится и вставляет iframe. */}
      <div
        ref={containerRef}
        className="flex min-h-[52px] items-center justify-center rounded-xl bg-white p-2 shadow-sm"
      />
      {authorizing && <p className="text-sm text-foreground/60">{t("authorizing")}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
