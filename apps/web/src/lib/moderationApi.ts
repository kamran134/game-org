// Как и venuesApi.ts/eventsApi.ts — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type ReportReason = "Spam" | "Abuse" | "FakeProfile" | "WrongInfo" | "InappropriateContent" | "Other";
export type ReportTargetType = "Venue" | "Event" | "User" | "Review";
export type ReportStatusValue = "Open" | "InReview" | "Resolved" | "Rejected";

export type ReportItem = {
  id: string;
  targetType: ReportTargetType;
  targetId: string;
  targetSummary: string;
  reason: ReportReason;
  comment?: string | null;
  status: ReportStatusValue;
  reporterId: string;
  reporterDisplayName: string;
  createdAt: string;
};

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

// Бэкенд резолвит мультиязычные поля (имя площадки/события/юзера в очереди
// жалоб) по этому заголовку. См. docs/PLAN.md, Шаг 7.5.
function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function createReport(
  apiUrl: string,
  locale: string,
  body: { targetType: ReportTargetType; targetId: string; reason: ReportReason; comment?: string | null },
): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/reports`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось отправить жалобу (${res.status})`));
}

export async function getReportQueue(apiUrl: string, locale: string): Promise<ReportItem[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/moderation/reports`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить жалобы (${res.status})`);
  return res.json();
}

export async function resolveReport(
  apiUrl: string,
  locale: string,
  id: string,
  body: { status: "Resolved" | "Rejected"; resolutionNote?: string | null },
): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/moderation/reports/${id}/resolve`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обработать жалобу (${res.status})`));
}

export async function banUser(apiUrl: string, locale: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/moderation/users/${userId}/ban`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось забанить пользователя (${res.status})`));
}

export async function unbanUser(apiUrl: string, locale: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/moderation/users/${userId}/unban`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось разбанить пользователя (${res.status})`));
}
