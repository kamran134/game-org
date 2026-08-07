export type AuthUser = { userId: string; handle: string; displayName: string };

export type SkillLevel = "Beginner" | "Amateur" | "Intermediate" | "Advanced" | "SemiPro" | "Pro";
export type Footedness = "Left" | "Right" | "Both";
export type Visibility = "Public" | "Followers" | "Private";
export type Gender = "Male" | "Female" | "Other";

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
  sports: UserSport[];
};

// NEXT_PUBLIC_API_URL несёт /api на dev (см. infra/compose.dev.yml), но не
// локально (.env.example) — нормализуем, чтобы оба случая давали один и тот
// же путь запроса.
function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function loginWithTelegram(apiUrl: string, telegramUser: unknown): Promise<AuthUser> {
  const res = await fetch(`${apiBase(apiUrl)}/api/auth/telegram`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(telegramUser),
  });
  if (!res.ok) throw new Error(`Не удалось войти через Telegram (${res.status})`);
  return res.json();
}

export async function fetchMe(apiUrl: string): Promise<MeProfile | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/me`, { credentials: "include" });
  if (res.status === 401) return null;
  if (!res.ok) throw new Error(`Не удалось получить профиль (${res.status})`);
  return res.json();
}

export type UpdateMeRequest = Partial<{
  displayName: string;
  bio: string;
  phone: string;
  locale: string;
  timezone: string;
  cityId: string;
  profileVisibility: Visibility;
  handle: string;
  birthDate: string;
  gender: Gender;
}>;

export async function updateMe(apiUrl: string, patch: UpdateMeRequest): Promise<MeProfile> {
  const res = await fetch(`${apiBase(apiUrl)}/api/me`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
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

export async function upsertMySport(apiUrl: string, sportId: string, body: UpsertUserSportRequest): Promise<UserSport> {
  const res = await fetch(`${apiBase(apiUrl)}/api/me/sports/${sportId}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось сохранить вид спорта (${res.status})`));
  return res.json();
}

export async function removeMySport(apiUrl: string, sportId: string): Promise<void> {
  const res = await fetch(`${apiBase(apiUrl)}/api/me/sports/${sportId}`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось удалить вид спорта (${res.status})`);
}

export async function logout(apiUrl: string): Promise<void> {
  await fetch(`${apiBase(apiUrl)}/api/auth/logout`, { method: "POST", credentials: "include" });
}
