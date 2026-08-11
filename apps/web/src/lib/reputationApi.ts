// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type LeaderboardEntry = {
  userId: string;
  handle: string;
  displayName: string;
  rating: number;
  gamesPlayed: number;
  wins: number;
  draws: number;
  losses: number;
};

// EarnedAt: null — ещё не получено (только в каталоге /api/me/achievements,
// в профиле — всегда заполнено, там только полученные).
export type Achievement = {
  code: string;
  name: string;
  description?: string | null;
  icon?: string | null;
  tier: number;
  earnedAt?: string | null;
};

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

export async function getLeaderboard(apiUrl: string, locale: string, sportSlug: string, take = 50): Promise<LeaderboardEntry[]> {
  const res = await fetch(`${apiBase(apiUrl)}/api/sports/${encodeURIComponent(sportSlug)}/leaderboard?take=${take}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить рейтинг (${res.status})`);
  return res.json();
}

export async function getMyAchievements(apiUrl: string, locale: string): Promise<Achievement[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/achievements`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить достижения (${res.status})`);
  return res.json();
}
