// Access-токен живёт 15 минут (TokenService.AccessTokenLifetime), а
// refresh-токен — 30 дней в отдельной httpOnly-куке. Без этой обёртки сессия
// молча умирает раньше срока: человек всё ещё залогинен (кука жива), но
// каждый запрос с протухшим access-токеном отвечает 401, и фронтенд решает,
// что сессии нет. На 401 один раз дёргаем /api/auth/refresh и повторяем
// исходный запрос.

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

let refreshInFlight: Promise<boolean> | null = null;

function refresh(apiUrl: string): Promise<boolean> {
  // Общий на все параллельные 401 — иначе несколько запросов, упавших
  // одновременно на один и тот же протухший токен, дёрнут refresh каждый по
  // отдельности, а ротация refresh-токена (TokenService.RefreshAsync)
  // инвалидирует все попытки, кроме первой.
  if (!refreshInFlight) {
    refreshInFlight = fetch(`${apiBase(apiUrl)}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    })
      .then((res) => res.ok)
      .catch(() => false)
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
}

/**
 * fetch с автоматическим восстановлением сессии на 401. НЕ использовать для:
 * uploadToPresignedUrl (льёт в R2, не в наш API), logout, и самого запроса
 * логина/refresh — иначе рекурсия или бессмысленный повтор.
 */
export async function fetchWithRefresh(apiUrl: string, url: string, init?: RequestInit): Promise<Response> {
  const res = await fetch(url, init);
  if (res.status !== 401) return res;

  const refreshed = await refresh(apiUrl);
  if (!refreshed) return res;

  return fetch(url, init);
}
