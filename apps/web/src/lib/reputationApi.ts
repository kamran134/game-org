// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

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
