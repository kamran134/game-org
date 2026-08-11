// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type FollowTargetType = "User" | "Club" | "Venue";

export type FollowSummary = {
  targetType: FollowTargetType;
  targetId: string;
  slug: string;
  name: string;
  avatarId?: string | null;
};

export type ActivityVerb =
  | "CreatedEvent"
  | "JoinedEvent"
  | "CompletedEvent"
  | "JoinedClub"
  | "CreatedClub"
  | "ReviewedVenue"
  | "EarnedAchievement"
  | "AddedSport"
  | "RatingMilestone";

export type ActivityActor = { id: string; handle: string; name: string; avatarId?: string | null };
export type ActivityEvent = { id: string; publicId: string; title?: string | null; startsAt: string };
export type ActivityClub = { id: string; slug: string; name: string };
export type ActivityVenue = { id: string; slug: string; name: string };

export type Activity = {
  id: string;
  actor: ActivityActor;
  verb: ActivityVerb;
  event?: ActivityEvent | null;
  club?: ActivityClub | null;
  venue?: ActivityVenue | null;
  targetUser?: ActivityActor | null;
  createdAt: string;
};

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function follow(apiUrl: string, targetType: FollowTargetType, targetId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/social/follow`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ targetType, targetId }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось подписаться (${res.status})`));
}

export async function unfollow(apiUrl: string, targetType: FollowTargetType, targetId: string): Promise<void> {
  const query = new URLSearchParams({ targetType, targetId });
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/social/follow?${query.toString()}`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!res.ok && res.status !== 404) throw new Error(await errorMessage(res, `Не удалось отписаться (${res.status})`));
}

export async function getFollowers(
  apiUrl: string,
  locale: string,
  targetType: FollowTargetType,
  targetId: string,
  params: { skip?: number; take?: number } = {},
): Promise<FollowSummary[]> {
  const query = new URLSearchParams({ targetType, targetId });
  if (params.skip) query.set("skip", String(params.skip));
  if (params.take) query.set("take", String(params.take));

  const res = await fetch(`${apiBase(apiUrl)}/api/social/followers?${query.toString()}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить подписчиков (${res.status})`);
  return res.json();
}

export async function getFollowing(
  apiUrl: string,
  locale: string,
  userId: string,
  params: { targetType?: FollowTargetType; skip?: number; take?: number } = {},
): Promise<FollowSummary[]> {
  const query = new URLSearchParams();
  if (params.targetType) query.set("targetType", params.targetType);
  if (params.skip) query.set("skip", String(params.skip));
  if (params.take) query.set("take", String(params.take));

  const res = await fetch(`${apiBase(apiUrl)}/api/social/following/${userId}?${query.toString()}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить подписки (${res.status})`);
  return res.json();
}

export async function getFeed(
  apiUrl: string,
  locale: string,
  params: { skip?: number; take?: number } = {},
): Promise<Activity[]> {
  const query = new URLSearchParams();
  if (params.skip) query.set("skip", String(params.skip));
  if (params.take) query.set("take", String(params.take));

  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/social/feed?${query.toString()}`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить ленту (${res.status})`);
  return res.json();
}
