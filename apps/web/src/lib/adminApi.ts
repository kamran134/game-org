// Как и moderationApi.ts/venuesApi.ts — Kiota здесь не используется (эндпоинты
// требуют cookie-авторизацию, анонимный Kiota-клиент её не носит), всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";
import type { UserRole } from "@/lib/authApi";

export type UserStatusValue = "Active" | "Suspended" | "Deactivated";

export type AdminOverview = { pendingReports: number; pendingVenues: number; pendingClaims: number };

export type AdminUser = {
  id: string;
  handle: string;
  displayName: string;
  avatarUrl?: string | null;
  role: UserRole;
  status: UserStatusValue;
  createdAt: string;
};

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function getAdminOverview(apiUrl: string): Promise<AdminOverview> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/admin/overview`, { credentials: "include" });
  if (!res.ok) throw new Error(`Не удалось загрузить сводку (${res.status})`);
  return res.json();
}

export async function searchAdminUsers(
  apiUrl: string,
  params: { query?: string; role?: UserRole; banned?: boolean; skip?: number; take?: number },
): Promise<AdminUser[]> {
  const qs = new URLSearchParams();
  if (params.query) qs.set("query", params.query);
  if (params.role) qs.set("role", params.role);
  if (params.banned !== undefined) qs.set("banned", String(params.banned));
  if (params.skip !== undefined) qs.set("skip", String(params.skip));
  if (params.take !== undefined) qs.set("take", String(params.take));

  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/admin/users?${qs.toString()}`, { credentials: "include" });
  if (!res.ok) throw new Error(`Не удалось загрузить пользователей (${res.status})`);
  return res.json();
}

export async function updateUserRole(apiUrl: string, userId: string, role: UserRole): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/admin/users/${userId}/role`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ role }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось изменить роль (${res.status})`));
}
