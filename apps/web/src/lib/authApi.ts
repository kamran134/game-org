import type { LocalizedText } from "@/lib/localized";
import { fetchWithRefresh } from "@/lib/fetchWithRefresh";
import type { Achievement } from "@/lib/reputationApi";

export type AuthUser = { userId: string; handle: string; displayName: string };

export type SkillLevel = "Beginner" | "Amateur" | "Intermediate" | "Advanced" | "SemiPro" | "Pro";
export type Footedness = "Left" | "Right" | "Both";
export type Visibility = "Public" | "Followers" | "Private";
export type Gender = "Male" | "Female" | "Other";
export type UserRole = "User" | "Moderator" | "Admin";

export type City = { id: string; slug: string; nameI18n: Record<string, string>; lat: number; lng: number };
export type UserSportPosition = { positionId: string; code: string; isPrimary: boolean };
export type UserSport = {
  sportId: string;
  sportSlug: string;
  sportEmoji?: string | null;
  level: SkillLevel;
  isPrimary: boolean;
  playingSince?: number | null;
  footedness?: Footedness | null;
  heightCm?: number | null;
  jerseyNumber?: number | null;
  note?: string | null;
  visibility: Visibility;
  positions: UserSportPosition[];
  rating: number;
  gamesPlayed: number;
  wins: number;
  draws: number;
  losses: number;
};
export type MeProfile = {
  id: string;
  handle: string;
  displayName: string;
  bio?: string | null;
  birthDate?: string | null;
  gender?: Gender | null;
  phone?: string | null;
  locale: string;
  timezone: string;
  profileVisibility: Visibility;
  city?: City | null;
  avatarId?: string | null;
  isVerified: boolean;
  role: UserRole;
  sports: UserSport[];
  followersCount: number;
  followingCount: number;
  reliabilityScore: number;
  achievements: Achievement[];
  // Резолвнутые displayName/bio выше — для отображения. Эти два — сырые
  // словари по всем языкам, только для формы редактирования (/me).
  displayNameI18n: LocalizedText;
  bioI18n?: LocalizedText | null;
};

// NEXT_PUBLIC_API_URL несёт /api на dev (см. infra/compose.dev.yml), но не
// локально (.env.example) — нормализуем, чтобы оба случая давали один и тот
// же путь запроса.
function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

// Бэкенд резолвит мультиязычные поля (Venue.Name, User.DisplayName, ...) по
// этому заголовку — без него всегда падает на дефолт сайта (az), даже если
// пользователь сидит на /ru. См. docs/PLAN.md, Шаг 7.5.
function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function loginWithTelegram(apiUrl: string, locale: string, telegramUser: unknown): Promise<AuthUser> {
  const res = await fetch(`${apiBase(apiUrl)}/api/auth/telegram`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(telegramUser),
  });
  if (!res.ok) throw new Error(`Не удалось войти через Telegram (${res.status})`);
  return res.json();
}

// Каждая секция страницы события (Payments, Teams, MVP, Report, ...) сама
// вызывает fetchMe при монтировании — без этого при одном визите на страницу
// улетает по отдельному GET /api/me на секцию. Делим один и тот же промис
// между всеми, кто позвал fetchMe, пока первый вызов ещё не завершился.
let inFlight: { key: string; promise: Promise<MeProfile | null> } | null = null;

export async function fetchMe(apiUrl: string, locale: string): Promise<MeProfile | null> {
  const key = `${apiUrl}|${locale}`;
  if (inFlight?.key === key) return inFlight.promise;

  const promise = (async () => {
    const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me`, {
      credentials: "include",
      headers: localeHeaders(locale),
    });
    if (res.status === 401) return null;
    if (!res.ok) throw new Error(`Не удалось получить профиль (${res.status})`);
    return res.json();
  })();

  inFlight = { key, promise };
  try {
    return await promise;
  } finally {
    if (inFlight?.promise === promise) inFlight = null;
  }
}

export type UpdateMeRequest = Partial<{
  displayName: LocalizedText;
  bio: LocalizedText;
  phone: string;
  locale: string;
  timezone: string;
  cityId: string;
  profileVisibility: Visibility;
  handle: string;
  birthDate: string;
  gender: Gender;
}>;

export async function updateMe(apiUrl: string, locale: string, patch: UpdateMeRequest): Promise<MeProfile> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(patch),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить профиль (${res.status})`));
  return res.json();
}

export type UpsertUserSportRequest = {
  level: SkillLevel;
  isPrimary: boolean;
  playingSince?: number | null;
  footedness?: Footedness | null;
  heightCm?: number | null;
  jerseyNumber?: number | null;
  note?: string | null;
  visibility: Visibility;
  positionIds: string[];
  primaryPositionId?: string | null;
};

export async function upsertMySport(
  apiUrl: string,
  locale: string,
  sportId: string,
  body: UpsertUserSportRequest,
): Promise<UserSport> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/sports/${sportId}`, {
    method: "PUT",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось сохранить вид спорта (${res.status})`));
  return res.json();
}

export async function removeMySport(apiUrl: string, locale: string, sportId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/sports/${sportId}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось удалить вид спорта (${res.status})`);
}

export async function logout(apiUrl: string): Promise<void> {
  await fetch(`${apiBase(apiUrl)}/api/auth/logout`, { method: "POST", credentials: "include" });
}
