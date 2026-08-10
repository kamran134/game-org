"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { VenuePublishQueue } from "./VenuePublishQueue";
import { ReportsQueue } from "./ReportsQueue";
import { VenueClaimsQueue } from "./VenueClaimsQueue";

type Tab = "venues" | "reports" | "claims";

export function ModerationTabs({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Moderation");
  const locale = useLocale();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [tab, setTab] = useState<Tab>("venues");

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  if (me === undefined) return <p className="text-foreground/70">{t("loading")}</p>;
  if (!me || (me.role !== "Moderator" && me.role !== "Admin")) {
    return <p className="text-foreground/70">{t("noAccess")}</p>;
  }

  const tabs: { id: Tab; label: string }[] = [
    { id: "venues", label: t("tabs.venues") },
    { id: "reports", label: t("tabs.reports") },
    { id: "claims", label: t("tabs.claims") },
  ];

  return (
    <div className="flex flex-col gap-6">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitle")}</h1>

      <div className="flex gap-2 border-b border-brand-border">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`cursor-pointer border-b-2 px-3 py-2 text-sm font-medium transition-colors duration-200 ${
              tab === item.id
                ? "border-brand-primary text-foreground"
                : "border-transparent text-foreground/60 hover:text-foreground"
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === "venues" && <VenuePublishQueue apiUrl={apiUrl} />}
      {tab === "reports" && <ReportsQueue apiUrl={apiUrl} />}
      {tab === "claims" && <VenueClaimsQueue apiUrl={apiUrl} />}
    </div>
  );
}
