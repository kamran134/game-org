"use client";

import { useEffect, useState } from "react";
import { useFormatter, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import {
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  notificationHref,
  type NotificationItem,
} from "@/lib/notificationsApi";

const PAGE_SIZE = 20;

export function NotificationsList({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Notifications");
  const format = useFormatter();

  const [items, setItems] = useState<NotificationItem[] | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getNotifications(apiUrl, { take: PAGE_SIZE })
      .then((result) => {
        setItems(result);
        setHasMore(result.length === PAGE_SIZE);
      })
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, t]);

  async function handleLoadMore() {
    if (!items) return;
    setLoadingMore(true);
    try {
      const more = await getNotifications(apiUrl, { skip: items.length, take: PAGE_SIZE });
      setItems((prev) => [...(prev ?? []), ...more]);
      setHasMore(more.length === PAGE_SIZE);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("loadError"));
    } finally {
      setLoadingMore(false);
    }
  }

  async function handleItemClick(item: NotificationItem) {
    if (item.readAt) return;
    await markNotificationRead(apiUrl, item.id);
    setItems((prev) => prev?.map((n) => (n.id === item.id ? { ...n, readAt: new Date().toISOString() } : n)) ?? null);
  }

  async function handleMarkAllRead() {
    await markAllNotificationsRead(apiUrl);
    setItems((prev) => prev?.map((n) => ({ ...n, readAt: n.readAt ?? new Date().toISOString() })) ?? null);
  }

  const hasUnread = items?.some((n) => !n.readAt) ?? false;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>
        {hasUnread && (
          <button type="button" onClick={handleMarkAllRead} className="cursor-pointer text-sm text-brand-primary hover:underline">
            {t("markAllRead")}
          </button>
        )}
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {items === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : items.length === 0 ? (
        <p className="text-foreground/70">{t("empty")}</p>
      ) : (
        <>
          <ul className="flex flex-col gap-2">
            {items.map((n) => {
              const href = notificationHref(n);
              const itemClassName = `flex w-full flex-col gap-1 rounded-2xl border border-brand-border bg-background px-5 py-4 text-left transition-colors duration-200 hover:border-brand-primary/40 ${
                n.readAt ? "" : "border-brand-primary/30"
              }`;
              const content = (
                <>
                  <span className={n.readAt ? "text-foreground/70" : "font-medium text-foreground"}>{n.body}</span>
                  <span className="text-xs text-foreground/50">
                    {format.dateTime(new Date(n.createdAt), { dateStyle: "medium", timeStyle: "short" })}
                  </span>
                </>
              );
              return (
                <li key={n.id}>
                  {href ? (
                    <Link href={href} onClick={() => handleItemClick(n)} className={`${itemClassName} cursor-pointer`}>
                      {content}
                    </Link>
                  ) : (
                    <button type="button" onClick={() => handleItemClick(n)} className={`${itemClassName} cursor-pointer`}>
                      {content}
                    </button>
                  )}
                </li>
              );
            })}
          </ul>

          {hasMore && (
            <button
              type="button"
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="cursor-pointer self-center rounded-full border border-brand-border px-5 py-2 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
            >
              {loadingMore ? t("loading") : t("loadMore")}
            </button>
          )}
        </>
      )}
    </div>
  );
}
