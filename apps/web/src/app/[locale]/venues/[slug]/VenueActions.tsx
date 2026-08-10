"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { createVenueClaim, deleteVenue, type VenueStatus } from "@/lib/venuesApi";

export function VenueActions({
  apiUrl,
  venueId,
  createdById,
  status,
}: {
  apiUrl: string;
  venueId: string;
  createdById?: string | null;
  status: VenueStatus;
}) {
  const t = useTranslations("Venues");
  const locale = useLocale();
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [claiming, setClaiming] = useState(false);
  const [claimed, setClaimed] = useState(false);
  const [claimError, setClaimError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === createdById || me.role === "Moderator" || me.role === "Admin");
  const canClaim = !!me && me.id !== createdById;

  async function handleDelete() {
    if (!window.confirm(t("deleteConfirm"))) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteVenue(apiUrl, locale, venueId);
      router.push("/venues");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("deleteError"));
      setDeleting(false);
    }
  }

  async function handleClaim() {
    setClaiming(true);
    setClaimError(null);
    try {
      await createVenueClaim(apiUrl, locale, venueId);
      setClaimed(true);
    } catch (err) {
      setClaimError(err instanceof Error ? err.message : t("claimError"));
    } finally {
      setClaiming(false);
    }
  }

  return (
    <div className="mt-4 flex flex-wrap items-center gap-3">
      {status === "Draft" && (
        <span className="rounded-full bg-amber-500/15 px-3 py-1 text-sm font-medium text-amber-600 dark:text-amber-400">
          {t("draftBadge")}
        </span>
      )}
      {canManage && (
        <button
          type="button"
          onClick={handleDelete}
          disabled={deleting}
          className="cursor-pointer text-sm font-medium text-red-600 transition-colors duration-200 hover:underline disabled:opacity-50 dark:text-red-400"
        >
          {deleting ? t("deleting") : t("delete")}
        </button>
      )}
      {canClaim && !claimed && (
        <button
          type="button"
          onClick={handleClaim}
          disabled={claiming}
          className="cursor-pointer text-sm font-medium text-foreground/60 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
        >
          {claiming ? t("claiming") : t("claimVenue")}
        </button>
      )}
      {claimed && <p className="text-sm text-foreground/60">{t("claimSent")}</p>}
      {error && <p className="w-full text-sm text-red-600">{error}</p>}
      {claimError && <p className="w-full text-sm text-red-600">{claimError}</p>}
    </div>
  );
}
