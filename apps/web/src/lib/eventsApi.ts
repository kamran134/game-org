// Как и venuesApi.ts — Kiota здесь не используется (не перегенерировать в
// этой среде), всё через обычный fetch. Анонимные GET можно звать и из
// Server Component.

import type { LocalizedText } from "@/lib/localized";
import { fetchWithRefresh } from "@/lib/fetchWithRefresh";
import type { PaymentSummary } from "@/lib/paymentsApi";

export type EventType = "Game" | "Training" | "Tournament" | "Friendly";
export type EventStatus = "Draft" | "Scheduled" | "Confirmed" | "Cancelled" | "Completed";
export type EventVisibility = "Public" | "Club" | "Unlisted";
export type SkillLevel = "Beginner" | "Amateur" | "Intermediate" | "Advanced" | "SemiPro" | "Pro";
export type GenderPolicy = "Any" | "MenOnly" | "WomenOnly" | "MixedRequired";
export type CostSplit = "Free" | "PerPlayer" | "Total";
export type ParticipationStatus =
  | "Confirmed" | "Maybe" | "Waitlisted" | "Declined" | "LateCancel" | "NoShow" | "Attended" | "PendingApproval";

export type EventSportItem = {
  id: string;
  slug: string;
  nameI18n: Record<string, string>;
  emoji?: string | null;
  hasPositions: boolean;
  isTeamSport: boolean;
};

export type EventVenueItem = { id: string; slug: string; name: string };

export type EventListItem = {
  id: string;
  publicId: string;
  type: EventType;
  status: EventStatus;
  visibility: EventVisibility;
  sport: EventSportItem;
  venue?: EventVenueItem | null;
  customLocation?: string | null;
  title?: string | null;
  startsAt: string;
  endsAt: string;
  timezone: string;
  maxParticipants?: number | null;
  confirmedCount: number;
  cost?: number | null;
  currency: string;
};

export type EventParticipant = {
  id: string;
  userId?: string | null;
  displayName: string;
  guestName?: string | null;
  status: ParticipationStatus;
  waitlistOrder?: number | null;
  joinedAt: string;
};

export type EventTeam = {
  id: string;
  name: string;
  colorHex?: string | null;
  score?: number | null;
  sortOrder: number;
  memberParticipantIds: string[];
};

export type EventResult = {
  summary?: string | null;
  standings?: Record<string, unknown>[] | null;
  mvpUserId?: string | null;
  mvpDisplayName?: string | null;
  recordedById?: string | null;
  recordedAt: string;
};

export type MvpTallyEntry = { userId: string; displayName: string; votes: number };

export type EventJoinRequestItem = { participantId: string; userId: string; displayName: string; requestedAt: string };

export type EventDetail = {
  id: string;
  publicId: string;
  type: EventType;
  status: EventStatus;
  visibility: EventVisibility;
  sport: EventSportItem;
  clubId?: string | null;
  venue?: EventVenueItem | null;
  customLocation?: string | null;
  title?: string | null;
  description?: string | null;
  startsAt: string;
  endsAt: string;
  timezone: string;
  minParticipants?: number | null;
  maxParticipants?: number | null;
  waitlistEnabled: boolean;
  requiresApproval: boolean;
  skillLevelMin?: SkillLevel | null;
  skillLevelMax?: SkillLevel | null;
  genderPolicy: GenderPolicy;
  ageMin?: number | null;
  ageMax?: number | null;
  costSplit: CostSplit;
  cost?: number | null;
  currency: string;
  registrationOpensAt?: string | null;
  registrationClosesAt?: string | null;
  lockHoursBeforeStart?: number | null;
  confirmedCount: number;
  maybeCount: number;
  waitlistCount: number;
  createdById?: string | null;
  cancelledAt?: string | null;
  cancelReason?: string | null;
  participants: EventParticipant[];
  teams: EventTeam[];
  result?: EventResult | null;
  mvpTally: MvpTallyEntry[];
  myMvpVote?: string | null;
  myPayment?: PaymentSummary | null;
  // Резолвнутые title/description выше — для страницы просмотра. Эти два —
  // сырые словари по всем языкам, только для формы редактирования.
  titleI18n?: LocalizedText | null;
  descriptionI18n?: LocalizedText | null;
};

export type CreateEventRequest = {
  type: EventType;
  visibility: EventVisibility;
  sportId: string;
  clubId?: string | null;
  venueId?: string | null;
  customLocation?: string | null;
  title?: LocalizedText | null;
  description?: LocalizedText | null;
  startsAt: string;
  endsAt: string;
  timezone?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
  waitlistEnabled?: boolean | null;
  requiresApproval?: boolean | null;
  skillLevelMin?: SkillLevel | null;
  skillLevelMax?: SkillLevel | null;
  genderPolicy?: GenderPolicy | null;
  ageMin?: number | null;
  ageMax?: number | null;
  costSplit?: CostSplit | null;
  cost?: number | null;
  currency?: string | null;
  lockHoursBeforeStart?: number | null;
};

// SportId/ClubId/Type осознанно неизменяемы после создания (см. UpdateEventRequest на бэкенде).
export type UpdateEventRequest = Partial<Omit<CreateEventRequest, "type" | "sportId" | "clubId">>;

export type TeamInput = { name: string; colorHex?: string | null; sortOrder: number; participantIds: string[] };
export type SetTeamsRequest = { teams: TeamInput[] };

export type StandingEntry = { userId: string; place: number; score?: number | null };
export type TeamScoreEntry = { teamId: string; score: number };
export type AttendanceStatus = "Attended" | "NoShow" | "LateCancel";
export type AttendanceEntry = { participantId: string; status: AttendanceStatus };
export type RecordResultRequest = {
  summary?: string | null;
  standings?: StandingEntry[] | null;
  teamScores?: TeamScoreEntry[] | null;
  attendance?: AttendanceEntry[] | null;
  mvpUserId?: string | null;
};

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

// Бэкенд резолвит мультиязычные поля по этому заголовку — без него всегда
// падает на дефолт сайта (az). См. docs/PLAN.md, Шаг 7.5.
function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function getEvents(
  apiUrl: string,
  locale: string,
  params: { sportId?: string; cityId?: string; upcoming?: boolean },
): Promise<EventListItem[]> {
  const query = new URLSearchParams();
  if (params.sportId) query.set("sportId", params.sportId);
  if (params.cityId) query.set("cityId", params.cityId);
  query.set("upcoming", params.upcoming === false ? "false" : "true");

  const res = await fetch(`${apiBase(apiUrl)}/api/events?${query.toString()}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить события (${res.status})`);
  return res.json();
}

export async function getEvent(apiUrl: string, locale: string, publicId: string): Promise<EventDetail | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/events/${encodeURIComponent(publicId)}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Не удалось загрузить событие (${res.status})`);
  return res.json();
}

// Кредитная версия — тот же GET, но с cookie. Анонимный getEvent (страница
// собирается на сервере без cookie) всегда отдаёт myMvpVote/myPayment
// пустыми — MyPaymentBanner дозапрашивает их на клиенте, чтобы не мигать
// неправильной суммой при первой отрисовке (в отличие от MVP-голоса, тут
// ошибиться в первом рендере — показать "не должен" вместо "должен" — цена
// выше, чем терпимый флик).
export async function getEventAuthed(apiUrl: string, locale: string, publicId: string): Promise<EventDetail | null> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${encodeURIComponent(publicId)}`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) return null;
  return res.json();
}

export async function createEvent(apiUrl: string, locale: string, body: CreateEventRequest): Promise<EventDetail> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось создать событие (${res.status})`));
  return res.json();
}

export async function updateEvent(apiUrl: string, locale: string, id: string, body: UpdateEventRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${id}`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить событие (${res.status})`));
}

export async function joinEvent(
  apiUrl: string,
  locale: string,
  eventId: string,
  status: "Confirmed" | "Maybe",
): Promise<EventParticipant> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/participants`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ status }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось записаться (${res.status})`));
  return res.json();
}

export async function leaveEvent(apiUrl: string, locale: string, eventId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/participants/me`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось отменить запись (${res.status})`);
}

export async function getEventJoinRequests(apiUrl: string, locale: string, eventId: string): Promise<EventJoinRequestItem[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/join-requests`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось загрузить заявки (${res.status})`));
  return res.json();
}

export async function approveEventJoinRequest(apiUrl: string, locale: string, eventId: string, participantId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/participants/${participantId}/approve`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось одобрить заявку (${res.status})`));
}

export async function rejectEventJoinRequest(apiUrl: string, locale: string, eventId: string, participantId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/participants/${participantId}/reject`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось отклонить заявку (${res.status})`));
}

export async function addEventGuest(
  apiUrl: string,
  locale: string,
  eventId: string,
  guestName: string,
): Promise<EventParticipant> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/guests`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ guestName }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось добавить гостя (${res.status})`));
  return res.json();
}

export async function deleteEvent(apiUrl: string, locale: string, eventId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось удалить событие (${res.status})`));
}

export async function cancelEvent(apiUrl: string, locale: string, eventId: string, reason?: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/cancel`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ reason: reason ?? null }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось отменить событие (${res.status})`));
}

export async function completeEvent(apiUrl: string, locale: string, eventId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/complete`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось завершить событие (${res.status})`));
}

export async function setEventTeams(apiUrl: string, locale: string, eventId: string, body: SetTeamsRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/teams`, {
    method: "PUT",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось сохранить команды (${res.status})`));
}

export async function voteMvp(apiUrl: string, locale: string, eventId: string, targetUserId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/mvp-vote`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ targetUserId }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось проголосовать (${res.status})`));
}

export async function recordResult(apiUrl: string, locale: string, eventId: string, body: RecordResultRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/result`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось записать результат (${res.status})`));
}
