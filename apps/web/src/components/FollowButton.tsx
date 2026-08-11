"use client";

import { useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { fetchMe } from "@/lib/authApi";
import { follow, unfollow, getFollowing, type FollowTargetType } from "@/lib/socialApi";

/**
 * Универсальная кнопка подписки — User/Club/Venue. Если вызывающая страница
 * уже знает viewerIsFollowing из собственного аутентифицированного запроса
 * (ClubView — getClubAuthed), передать initialIsFollowing, чтобы не дёргать
 * список подписок ещё раз. Иначе (анонимная SSR-страница — /venues/[slug],
 * /[handle]) кнопка сама разберётся на клиенте: fetchMe → есть ли targetId
 * среди подписок viewer'а. Для анонимных/неавторизованных не рендерится.
 */
export function FollowButton({
  apiUrl,
  targetType,
  targetId,
  initialIsFollowing,
  onChange,
}: {
  apiUrl: string;
  targetType: FollowTargetType;
  targetId: string;
  initialIsFollowing?: boolean;
  onChange?: (isFollowing: boolean) => void;
}) {
  const t = useTranslations("Social");
  const locale = useLocale();
  const initialIsFollowingRef = useRef(initialIsFollowing);

  const [viewerId, setViewerId] = useState<string | null | undefined>(undefined);
  const [isFollowing, setIsFollowing] = useState(initialIsFollowing ?? false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then((me) => {
        setViewerId(me?.id ?? null);
        if (me && initialIsFollowingRef.current === undefined) {
          getFollowing(apiUrl, locale, me.id, { targetType })
            .then((list) => setIsFollowing(list.some((f) => f.targetId === targetId)))
            .catch(() => {});
        }
      })
      .catch(() => setViewerId(null));
  }, [apiUrl, locale, targetType, targetId]);

  if (!viewerId || viewerId === targetId) return null;

  async function toggle() {
    setBusy(true);
    setError(null);
    try {
      if (isFollowing) {
        await unfollow(apiUrl, targetType, targetId);
        setIsFollowing(false);
        onChange?.(false);
      } else {
        await follow(apiUrl, targetType, targetId);
        setIsFollowing(true);
        onChange?.(true);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t("actionError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={toggle}
        disabled={busy}
        className={
          isFollowing
            ? "cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-red-400 hover:text-red-600 disabled:opacity-50 dark:hover:text-red-400"
            : "cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
        }
      >
        {isFollowing ? t("following") : t("follow")}
      </button>
      {error && <span className="text-sm text-red-600">{error}</span>}
    </div>
  );
}
