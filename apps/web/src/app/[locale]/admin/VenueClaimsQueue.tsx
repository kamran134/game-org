"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { approveVenueClaim, getVenueClaimQueue, rejectVenueClaim, type VenueClaim } from "@/lib/venuesApi";

export function VenueClaimsQueue({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Moderation");
  const locale = useLocale();

  const [claims, setClaims] = useState<VenueClaim[] | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadQueue = useCallback(() => {
    getVenueClaimQueue(apiUrl, locale)
      .then(setClaims)
      .catch(() => setError(t("actionError")));
  }, [apiUrl, locale, t]);

  useEffect(() => {
    loadQueue();
  }, [loadQueue]);

  async function handleAction(id: string, action: "approve" | "reject") {
    setBusyId(id);
    setError(null);
    try {
      await (action === "approve" ? approveVenueClaim(apiUrl, locale, id) : rejectVenueClaim(apiUrl, locale, id));
      setClaims((prev) => prev?.filter((c) => c.id !== id) ?? null);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {error && <p className="text-sm text-red-600">{error}</p>}

      {claims === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : claims.length === 0 ? (
        <p className="text-foreground/70">{t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {claims.map((claim) => (
            <li
              key={claim.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-5 py-4"
            >
              <div>
                <span className="font-medium text-foreground">{claim.venueName}</span>
                <p className="text-sm text-foreground/60">{t("claimBy", { name: claim.userDisplayName })}</p>
                {claim.evidence && <p className="mt-1 text-sm text-foreground/70">{claim.evidence}</p>}
              </div>
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => handleAction(claim.id, "approve")}
                  disabled={busyId === claim.id}
                  className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
                >
                  {t("approve")}
                </button>
                <button
                  type="button"
                  onClick={() => handleAction(claim.id, "reject")}
                  disabled={busyId === claim.id}
                  className="cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
                >
                  {t("reject")}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
