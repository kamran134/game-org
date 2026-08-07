"use client";

import { useEffect, useRef, useState } from "react";
import { fetchMe, loginWithTelegram, type MeUser } from "@/lib/authApi";

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
  const containerRef = useRef<HTMLDivElement>(null);
  const [me, setMe] = useState<MeUser | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl)
      .then(setMe)
      .catch(() => {});
  }, [apiUrl]);

  useEffect(() => {
    if (me || !containerRef.current) return;

    window.onTelegramAuth = (user) => {
      loginWithTelegram(apiUrl, user)
        .then(() => fetchMe(apiUrl))
        .then(setMe)
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
  }, [apiUrl, botUsername, me]);

  if (me) {
    return (
      <p>
        Вы вошли как {me.displayName} (@{me.handle}).
      </p>
    );
  }

  return (
    <div className="flex flex-col items-center gap-3">
      <div ref={containerRef} />
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
