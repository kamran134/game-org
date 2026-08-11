// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

// Только реально вызываемые типы — см. NotificationService.WiredTypes на бэкенде.
export type NotificationType =
  | "EventReminder24h"
  | "EventReminder2h"
  | "EventUpdated"
  | "EventCancelled"
  | "EventConfirmed"
  | "ParticipantJoined"
  | "ParticipantLeft"
  | "WaitlistPromoted"
  | "ClubInvite"
  | "ClubJoinRequest"
  | "NewFollower"
  | "MvpVoteOpen"
  | "ResultPosted"
  | "AchievementEarned";

export type NotificationChannel = "Telegram" | "Push";

export type NotificationItem = {
  id: string;
  type: NotificationType;
  title?: string | null;
  body?: string | null;
  data?: Record<string, unknown> | null;
  createdAt: string;
  readAt?: string | null;
};

export type NotificationPreference = { type: NotificationType; enabled: boolean };

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

export async function getNotifications(
  apiUrl: string,
  params: { unreadOnly?: boolean; skip?: number; take?: number } = {},
): Promise<NotificationItem[]> {
  const query = new URLSearchParams();
  if (params.unreadOnly) query.set("unreadOnly", "true");
  if (params.skip !== undefined) query.set("skip", String(params.skip));
  if (params.take !== undefined) query.set("take", String(params.take));

  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/notifications?${query.toString()}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Не удалось загрузить уведомления (${res.status})`);
  return res.json();
}

export async function getUnreadNotificationCount(apiUrl: string): Promise<number> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/notifications/unread-count`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Не удалось загрузить счётчик уведомлений (${res.status})`);
  const data = await res.json();
  return data.count as number;
}

export async function markNotificationRead(apiUrl: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/notifications/${id}/read`, {
    method: "POST",
    credentials: "include",
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось отметить уведомление прочитанным (${res.status})`);
}

export async function markAllNotificationsRead(apiUrl: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/notifications/read-all`, {
    method: "POST",
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Не удалось отметить уведомления прочитанными (${res.status})`);
}

export async function getNotificationPreferences(apiUrl: string, channel: NotificationChannel = "Telegram"): Promise<NotificationPreference[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/notification-preferences?channel=${channel}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(`Не удалось загрузить настройки уведомлений (${res.status})`);
  return res.json();
}

export async function setNotificationPreference(
  apiUrl: string,
  type: NotificationType,
  enabled: boolean,
  channel: NotificationChannel = "Telegram",
): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/notification-preferences/${type}?channel=${channel}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ enabled }),
  });
  if (!res.ok) throw new Error(`Не удалось сохранить настройку (${res.status})`);
}
