"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
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
  const router = useRouter();
  const containerRef = useRef<HTMLDivElement>(null);
  const [checkingSession, setCheckingSession] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Уже залогинен — сразу на /me, виджет не нужен.
  useEffect(() => {
    fetchMe(apiUrl)
      .then((profile: MeProfile | null) => {
        if (profile) router.push("/me");
        else setCheckingSession(false);
      })
      .catch(() => setCheckingSession(false));
  }, [apiUrl, router]);

  useEffect(() => {
    if (checkingSession || !containerRef.current) return;

    window.onTelegramAuth = (user) => {
      loginWithTelegram(apiUrl, user)
        .then(() => router.push("/me"))
        .catch((err) => setError(err instanceof Error ? err.message : "Не удалось войти"));
    };

    // Виджет сам вставляет себя (iframe-кнопку) на место этого <script> —
    // поэтому вставляем тег вручную в контейнер, а не через next/script
    // (тот может переместить скрипт в другое место DOM).
    const script = document.createElement("script");
    script.src = "https://telegram.org/js/telegram-widget.js?22";
    script.async = true;
    script.setAttribute("data-telegram-login", botUsername);
    script.setAttribute("data-size", "large");
    script.setAttribute("data-onauth", "onTelegramAuth(user)");
    script.setAttribute("data-request-access", "write");
    containerRef.current.appendChild(script);

    return () => {
      window.onTelegramAuth = undefined;
    };
  }, [apiUrl, botUsername, checkingSession, router]);

  if (checkingSession) return null;

  return (
    <div className="flex flex-col items-center gap-3">
      <div ref={containerRef} />
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
