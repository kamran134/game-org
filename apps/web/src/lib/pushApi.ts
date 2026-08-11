// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

/** null — VAPID не настроен на бэкенде (нет секретов, см. docs/PLAN.md §17 — ручной шаг). */
export async function getVapidPublicKey(apiUrl: string): Promise<string | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/push/vapid-public-key`, { cache: "no-store" });
  if (!res.ok) return null;
  const data = await res.json();
  return data.publicKey as string;
}

// Token — целиком JSON.stringify(PushSubscription), не opaque-строка (Web Push API так и отдаёт).
export async function registerDevice(apiUrl: string, token: string, locale: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/devices`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, locale }),
  });
  if (!res.ok) throw new Error(`Не удалось подписаться на push-уведомления (${res.status})`);
}

export async function unregisterDevice(apiUrl: string, token: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/devices`, {
    method: "DELETE",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token }),
  });
  if (!res.ok) throw new Error(`Не удалось отписаться от push-уведомлений (${res.status})`);
}
