"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { MapPinLine, Flag, FileText } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { getAdminOverview, type AdminOverview } from "@/lib/adminApi";

const CARDS = [
  { key: "pendingVenues", href: "/admin/venues", icon: MapPinLine },
  { key: "pendingReports", href: "/admin/reports", icon: Flag },
  { key: "pendingClaims", href: "/admin/claims", icon: FileText },
] as const;

export function OverviewSection({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Admin");
  const [overview, setOverview] = useState<AdminOverview | null | undefined>(undefined);

  useEffect(() => {
    getAdminOverview(apiUrl)
      .then(setOverview)
      .catch(() => setOverview(null));
  }, [apiUrl]);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("sidebar.overview")}</h1>

      {overview === undefined ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : overview === null ? (
        <p className="text-foreground/70">{t("overview.loadError")}</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {CARDS.map(({ key, href, icon: Icon }) => (
            <Link
              key={key}
              href={href}
              className="flex flex-col gap-3 rounded-2xl border border-brand-border bg-background p-6 transition-colors duration-200 hover:border-brand-primary/40"
            >
              <Icon size={22} weight="bold" className="text-brand-primary" />
              <span className="font-heading text-3xl font-semibold tabular-nums text-foreground">{overview[key]}</span>
              <span className="text-sm text-foreground/60">{t(`overview.${key}`)}</span>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
