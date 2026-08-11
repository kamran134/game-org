// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import type { LocalizedText } from "@/lib/localized";
import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type ClubVisibility = "Public" | "RequestOnly" | "Private";
export type ClubKind = "Club" | "Group";
export type ClubRole = "Owner" | "Admin" | "Member";
export type MembershipStatus = "Pending" | "Active" | "Banned" | "Left";

export type ClubCity = { id: string; slug: string; nameI18n: Record<string, string>; lat: number; lng: number };

export type ClubListItem = {
  id: string;
  slug: string;
  name: string;
  city?: ClubCity | null;
  visibility: ClubVisibility;
  kind: ClubKind;
  avatarUrl?: string | null;
  membersCount: number;
  eventsCount: number;
};

export type ClubSportItem = {
  id: string;
  slug: string;
  nameI18n: Record<string, string>;
  emoji?: string | null;
  hasPositions: boolean;
  isTeamSport: boolean;
};

export type ClubDetail = {
  id: string;
  slug: string;
  name: string;
  description?: string | null;
  city?: ClubCity | null;
  visibility: ClubVisibility;
  kind: ClubKind;
  avatarUrl?: string | null;
  membersCount: number;
  eventsCount: number;
  createdById?: string | null;
  // Только для Owner/Admin — иначе null.
  inviteCode?: string | null;
  viewerRole?: ClubRole | null;
  viewerStatus?: MembershipStatus | null;
  followersCount: number;
  viewerIsFollowing: boolean;
  sports: ClubSportItem[];
  // Резолвнутые name/description выше — для просмотра. Эти два — сырые
  // словари по всем языкам, только для формы редактирования.
  nameI18n: LocalizedText;
  descriptionI18n?: LocalizedText | null;
};

export type ClubMember = {
  userId: string;
  displayName: string;
  role: ClubRole;
  status: MembershipStatus;
  joinedAt: string;
};

export type CreateClubRequest = {
  name: LocalizedText;
  description?: LocalizedText | null;
  cityId?: string | null;
  visibility?: ClubVisibility | null;
  // Неизменяем после создания — как sportId у событий, см. ClubDtos.cs.
  kind?: ClubKind | null;
  sportIds?: string[] | null;
};

export type UpdateClubRequest = Partial<Omit<CreateClubRequest, "kind">>;

export type PresignClubAvatarResponse = { mediaId: string; uploadUrl: string; publicUrl: string };

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

// Бэкенд резолвит мультиязычные поля по этому заголовку. См. docs/PLAN.md, Шаг 7.5.
function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function getClubs(
  apiUrl: string,
  locale: string,
  params: { cityId?: string; sportId?: string; kind?: ClubKind } = {},
): Promise<ClubListItem[]> {
  const query = new URLSearchParams();
  if (params.cityId) query.set("cityId", params.cityId);
  if (params.sportId) query.set("sportId", params.sportId);
  if (params.kind) query.set("kind", params.kind);

  const res = await fetch(`${apiBase(apiUrl)}/api/clubs?${query.toString()}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить клубы (${res.status})`);
  return res.json();
}

export async function getClub(apiUrl: string, locale: string, slug: string): Promise<ClubDetail | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/clubs/${encodeURIComponent(slug)}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Не удалось загрузить клуб (${res.status})`);
  return res.json();
}

// Кредитная версия getClub — с cookie. Нужна для Private-клуба и для
// InviteCode/ViewerRole: анонимный getClub из Server Component для них
// вернёт либо 404 (Private не-участнику), либо просто не отдаст inviteCode.
export async function getClubAuthed(apiUrl: string, locale: string, slug: string): Promise<ClubDetail | null> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${encodeURIComponent(slug)}`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) return null;
  return res.json();
}

// Клубы, где текущий пользователь активный участник — для селектора клуба в
// форме события (ClubId требует активного членства).
export async function getMyClubs(apiUrl: string, locale: string): Promise<ClubListItem[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/mine`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) return [];
  return res.json();
}

export async function createClub(apiUrl: string, locale: string, body: CreateClubRequest): Promise<ClubDetail> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось создать клуб (${res.status})`));
  return res.json();
}

export async function updateClub(apiUrl: string, locale: string, id: string, body: UpdateClubRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить клуб (${res.status})`));
}

export async function presignClubAvatar(apiUrl: string, locale: string, id: string, contentType: string): Promise<PresignClubAvatarResponse> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/avatar/presign`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ contentType }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось подготовить загрузку (${res.status})`));
  return res.json();
}

export async function attachClubAvatar(apiUrl: string, locale: string, id: string, mediaId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/avatar`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ mediaId }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось сохранить логотип (${res.status})`));
}

export async function deleteClub(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось удалить клуб (${res.status})`));
}

export async function joinClub(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/join`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось вступить в клуб (${res.status})`));
}

export async function leaveClub(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/leave`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось покинуть клуб (${res.status})`));
}

export async function getClubMembers(apiUrl: string, locale: string, id: string): Promise<ClubMember[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/members`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить участников (${res.status})`);
  return res.json();
}

export async function getClubJoinRequests(apiUrl: string, locale: string, id: string): Promise<ClubMember[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/join-requests`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить заявки (${res.status})`);
  return res.json();
}

export async function approveJoinRequest(apiUrl: string, locale: string, id: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/join-requests/${userId}/approve`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось одобрить заявку (${res.status})`));
}

export async function rejectJoinRequest(apiUrl: string, locale: string, id: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/join-requests/${userId}/reject`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось отклонить заявку (${res.status})`));
}

export async function removeMember(apiUrl: string, locale: string, id: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/members/${userId}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось удалить участника (${res.status})`));
}

export async function setMemberRole(apiUrl: string, locale: string, id: string, userId: string, role: ClubRole): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/members/${userId}`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ role }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось изменить роль (${res.status})`));
}

export async function transferOwnership(apiUrl: string, locale: string, id: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/members/${userId}/transfer-ownership`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось передать владение (${res.status})`));
}

export async function inviteMember(apiUrl: string, locale: string, id: string, userId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/invite`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ userId }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось пригласить участника (${res.status})`));
}

export async function joinClubByInviteCode(apiUrl: string, locale: string, code: string): Promise<{ slug: string }> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/join/${encodeURIComponent(code)}`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось вступить по коду (${res.status})`));
  return res.json();
}

export async function regenerateInviteCode(apiUrl: string, locale: string, id: string): Promise<{ code: string }> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/clubs/${id}/invite-code/regenerate`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить код (${res.status})`));
  return res.json();
}
