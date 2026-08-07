export type AuthUser = { userId: string; handle: string; displayName: string };
export type MeUser = { id: string; handle: string; displayName: string; locale: string };

// NEXT_PUBLIC_API_URL несёт /api на dev (см. infra/compose.dev.yml), но не
// локально (.env.example) — нормализуем, чтобы оба случая давали один и тот
// же путь запроса.
function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
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

export async function fetchMe(apiUrl: string): Promise<MeUser | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/me`, { credentials: "include" });
  if (res.status === 401) return null;
  if (!res.ok) throw new Error(`Не удалось получить профиль (${res.status})`);
  return res.json();
}

export async function logout(apiUrl: string): Promise<void> {
  await fetch(`${apiBase(apiUrl)}/api/auth/logout`, { method: "POST", credentials: "include" });
}
