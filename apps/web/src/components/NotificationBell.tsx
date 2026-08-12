"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Bell } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import {
  getNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  markNotificationRead,
  notificationHref,
  type NotificationItem,
} from "@/lib/notificationsApi";

// count === null и до первого, и до не-логина — колокольчик рендерится только
// у залогиненного пользователя, поэтому родитель (UserMenu) уже гарантирует
// это условием рендера; здесь null просто значит "ещё не знаем/ошибка".
export function NotificationBell({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Notifications");

  const [count, setCount] = useState<number | null>(null);
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<NotificationItem[] | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  const loadCount = useCallback(() => {
    getUnreadNotificationCount(apiUrl)
      .then(setCount)
      .catch(() => setCount(null));
  }, [apiUrl]);

  useEffect(() => {
    loadCount();
  }, [loadCount]);

  useEffect(() => {
    if (!open) return;
    getNotifications(apiUrl, { take: 10 })
      .then(setItems)
      .catch(() => setItems([]));
  }, [apiUrl, open]);

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  async function handleItemClick(item: NotificationItem) {
    if (item.readAt) return;
    await markNotificationRead(apiUrl, item.id);
    setItems((prev) => prev?.map((n) => (n.id === item.id ? { ...n, readAt: new Date().toISOString() } : n)) ?? null);
    setCount((c) => (c && c > 0 ? c - 1 : 0));
  }

  async function handleMarkAllRead() {
    await markAllNotificationsRead(apiUrl);
    setItems((prev) => prev?.map((n) => ({ ...n, readAt: n.readAt ?? new Date().toISOString() })) ?? null);
    setCount(0);
  }

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t("bell")}
        className="relative flex h-8 w-8 cursor-pointer items-center justify-center text-foreground/70 transition-colors duration-200 hover:text-foreground"
      >
        <Bell size={19} weight="bold" />
        {!!count && count > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-primary px-1 text-[10px] font-semibold text-brand-primary-foreground">
            {count > 9 ? "9+" : count}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-10 z-50 w-80 rounded-2xl border border-brand-border bg-background shadow-lg">
          <div className="flex items-center justify-between border-b border-brand-border px-4 py-3">
            <span className="text-sm font-semibold text-foreground">{t("title")}</span>
            {!!count && count > 0 && (
              <button type="button" onClick={handleMarkAllRead} className="cursor-pointer text-xs text-brand-primary hover:underline">
                {t("markAllRead")}
              </button>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto">
            {items === null ? (
              <p className="px-4 py-4 text-sm text-foreground/60">{t("loading")}</p>
            ) : items.length === 0 ? (
              <p className="px-4 py-4 text-sm text-foreground/60">{t("empty")}</p>
            ) : (
              <ul>
                {items.map((n) => {
                  const href = notificationHref(n);
                  const itemClassName = `block w-full px-4 py-3 text-left text-sm transition-colors duration-200 hover:bg-brand-muted ${
                    n.readAt ? "text-foreground/60" : "font-medium text-foreground"
                  }`;
                  return (
                    <li key={n.id}>
                      {href ? (
                        <Link
                          href={href}
                          onClick={() => {
                            setOpen(false);
                            handleItemClick(n);
                          }}
                          className={`${itemClassName} cursor-pointer`}
                        >
                          {n.body}
                        </Link>
                      ) : (
                        <button type="button" onClick={() => handleItemClick(n)} className={`${itemClassName} cursor-pointer`}>
                          {n.body}
                        </button>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <Link
            href="/notifications"
            onClick={() => setOpen(false)}
            className="block border-t border-brand-border px-4 py-3 text-center text-sm font-medium text-brand-primary hover:underline"
          >
            {t("viewAll")}
          </Link>
        </div>
      )}
    </div>
  );
}
