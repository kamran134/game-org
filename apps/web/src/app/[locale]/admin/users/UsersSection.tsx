"use client";

import { useEffect, useState } from "react";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { banUser, unbanUser } from "@/lib/moderationApi";
import { searchAdminUsers, updateUserRole, type AdminUser } from "@/lib/adminApi";
import type { UserRole } from "@/lib/authApi";
import { useAdminViewer } from "../AdminRoleContext";

const PAGE_SIZE = 20;
const ROLES: UserRole[] = ["User", "Moderator", "Admin"];

// Тот же приём, что в EventsList/ClubsList (Шаг 21): фильтры живут в URL,
// значения приходят пропами со страницы (Server Component уже прочитал
// searchParams), здесь нет useSearchParams() — именно он ломал сборку
// в Docker. Текстовый поиск — единственное исключение с локальным
// состоянием, но и оно синхронизируется в URL с debounce, а не живёт
// только в компоненте.
export function UsersSection({
  apiUrl,
  query,
  role,
  banned,
  search,
}: {
  apiUrl: string;
  query?: string;
  role?: string;
  banned?: string;
  search: Record<string, string | undefined>;
}) {
  const t = useTranslations("Admin");
  const format = useFormatter();
  const locale = useLocale();
  const router = useRouter();
  const viewer = useAdminViewer();

  const [queryInput, setQueryInput] = useState(query ?? "");
  const [prevQuery, setPrevQuery] = useState(query);
  const [result, setResult] = useState<{ key: string; items: AdminUser[]; hasMore: boolean } | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const roleFilter = role === "User" || role === "Moderator" || role === "Admin" ? role : undefined;
  const bannedFilter = banned === "1" ? true : banned === "0" ? false : undefined;
  const filterKey = `${query ?? ""}|${roleFilter ?? ""}|${bannedFilter ?? ""}`;

  // Синхронизация текстового поля с URL при навигации назад/вперёд —
  // сброс прямо в теле рендера (не в эффекте), тот же приём, что в
  // ParticipantsSection (Шаг 19): производное от пропа состояние.
  if (prevQuery !== query) {
    setPrevQuery(query);
    setQueryInput(query ?? "");
  }

  useEffect(() => {
    let cancelled = false;
    searchAdminUsers(apiUrl, { query, role: roleFilter, banned: bannedFilter, take: PAGE_SIZE })
      .then((items) => {
        if (!cancelled) setResult({ key: filterKey, items, hasMore: items.length === PAGE_SIZE });
      })
      .catch(() => {
        if (!cancelled) setError(t("users.loadError"));
      });
    return () => {
      cancelled = true;
    };
  }, [apiUrl, query, roleFilter, bannedFilter, filterKey, t]);

  const ready = result !== null && result.key === filterKey;
  const users = ready ? result!.items : null;

  function hrefWith(overrides: Record<string, string | null>): string {
    const next = new URLSearchParams();
    for (const [key, value] of Object.entries(search)) {
      if (value !== undefined) next.set(key, value);
    }
    for (const [key, value] of Object.entries(overrides)) {
      if (value === null) next.delete(key);
      else next.set(key, value);
    }
    const qs = next.toString();
    return qs ? `/admin/users?${qs}` : "/admin/users";
  }

  // Debounce — иначе каждая нажатая клавиша дёргает навигацию.
  useEffect(() => {
    const trimmed = queryInput.trim();
    if (trimmed === (query ?? "")) return;
    const timeout = setTimeout(() => {
      router.replace(hrefWith({ query: trimmed || null }));
    }, 350);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queryInput]);

  async function handleLoadMore() {
    if (!result) return;
    setLoadingMore(true);
    try {
      const more = await searchAdminUsers(apiUrl, { query, role: roleFilter, banned: bannedFilter, skip: result.items.length, take: PAGE_SIZE });
      setResult((prev) => (prev ? { ...prev, items: [...prev.items, ...more], hasMore: more.length === PAGE_SIZE } : prev));
    } catch {
      setError(t("users.loadError"));
    } finally {
      setLoadingMore(false);
    }
  }

  async function handleBanToggle(user: AdminUser) {
    setBusyId(user.id);
    setError(null);
    try {
      await (user.status === "Suspended" ? unbanUser(apiUrl, locale, user.id) : banUser(apiUrl, locale, user.id));
      setResult((prev) =>
        prev
          ? {
              ...prev,
              items: prev.items.map((u) => (u.id === user.id ? { ...u, status: u.status === "Suspended" ? "Active" : "Suspended" } : u)),
            }
          : prev,
      );
    } catch {
      setError(t("users.actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRoleChange(user: AdminUser, newRole: UserRole) {
    setBusyId(user.id);
    setError(null);
    try {
      await updateUserRole(apiUrl, user.id, newRole);
      setResult((prev) => (prev ? { ...prev, items: prev.items.map((u) => (u.id === user.id ? { ...u, role: newRole } : u)) } : prev));
    } catch {
      setError(t("users.actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <input
          value={queryInput}
          onChange={(e) => setQueryInput(e.target.value)}
          placeholder={t("users.searchPlaceholder")}
          className="min-w-48 flex-1 rounded-full border border-brand-border bg-background px-4 py-2 text-sm text-foreground placeholder:text-foreground/40 focus:border-brand-primary/40 focus:outline-none"
        />
        <select
          value={roleFilter ?? ""}
          onChange={(e) => router.replace(hrefWith({ role: e.target.value || null }))}
          className="rounded-full border border-brand-border bg-background px-3 py-2 text-sm text-foreground"
        >
          <option value="">{t("users.allRoles")}</option>
          {ROLES.map((r) => (
            <option key={r} value={r}>
              {t(`users.roles.${r}`)}
            </option>
          ))}
        </select>
        <select
          value={banned ?? ""}
          onChange={(e) => router.replace(hrefWith({ banned: e.target.value || null }))}
          className="rounded-full border border-brand-border bg-background px-3 py-2 text-sm text-foreground"
        >
          <option value="">{t("users.allStatuses")}</option>
          <option value="0">{t("users.active")}</option>
          <option value="1">{t("users.banned")}</option>
        </select>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {users === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : users.length === 0 ? (
        <p className="text-foreground/70">{t("users.empty")}</p>
      ) : (
        <>
          <ul className="flex flex-col gap-2">
            {users.map((user) => {
              const isSelf = viewer?.userId === user.id;
              const canChangeRole = viewer?.role === "Admin" && !isSelf;
              // Разбанить может любой модератор — ограничение на бэкенде (только Admin
              // банит модератора/админа) действует лишь для самого бана, не для снятия.
              const canBan = user.status === "Suspended" || viewer?.role === "Admin" || user.role === "User";
              return (
                <li
                  key={user.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-5 py-4"
                >
                  <div className="flex items-center gap-3">
                    {user.avatarUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={user.avatarUrl} alt="" width={36} height={36} className="h-9 w-9 rounded-full object-cover" />
                    ) : (
                      <div className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-muted text-sm font-semibold text-foreground/60">
                        {user.displayName.charAt(0).toUpperCase()}
                      </div>
                    )}
                    <div>
                      <p className="font-medium text-foreground">
                        {user.displayName} <span className="text-foreground/40">@{user.handle}</span>
                      </p>
                      <p className="text-xs text-foreground/50">
                        {t("users.joined", { date: format.dateTime(new Date(user.createdAt), { dateStyle: "medium" }) })}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    {user.status === "Suspended" && (
                      <span className="rounded-full bg-red-500/10 px-2.5 py-0.5 text-xs font-medium text-red-600 dark:text-red-400">
                        {t("users.banned")}
                      </span>
                    )}

                    {canChangeRole ? (
                      <select
                        value={user.role}
                        disabled={busyId === user.id}
                        onChange={(e) => handleRoleChange(user, e.target.value as UserRole)}
                        className="rounded-full border border-brand-border bg-background px-3 py-1.5 text-sm text-foreground disabled:opacity-50"
                      >
                        {ROLES.map((r) => (
                          <option key={r} value={r}>
                            {t(`users.roles.${r}`)}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <span className="rounded-full bg-brand-muted px-2.5 py-0.5 text-xs font-medium text-brand-foreground">
                        {t(`users.roles.${user.role}`)}
                      </span>
                    )}

                    {canBan && (
                      <button
                        type="button"
                        onClick={() => handleBanToggle(user)}
                        disabled={busyId === user.id}
                        className={`cursor-pointer rounded-full border px-4 py-1.5 text-sm font-medium transition-colors duration-200 disabled:opacity-50 ${
                          user.status === "Suspended"
                            ? "border-brand-border text-foreground/70 hover:text-foreground"
                            : "border-red-500/30 text-red-600 hover:bg-red-500/10 dark:text-red-400"
                        }`}
                      >
                        {user.status === "Suspended" ? t("users.unban") : t("users.ban")}
                      </button>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>

          {result?.hasMore && (
            <button
              type="button"
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="cursor-pointer self-center rounded-full border border-brand-border px-5 py-2 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
            >
              {loadingMore ? t("loading") : t("users.loadMore")}
            </button>
          )}
        </>
      )}
    </div>
  );
}
