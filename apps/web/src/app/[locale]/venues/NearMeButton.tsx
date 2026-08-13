"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { MapPinLine } from "@phosphor-icons/react";
import { useRouter } from "@/i18n/navigation";
import { buildFilterHref } from "@/lib/filterHref";

// Складывает координаты поверх остальных фильтров (Шаг 24) — раньше
// перезаписывала весь query одними lat/lng, из-за чего выбранные вид
// спорта/город/крытость слетали при клике "рядом со мной".
export function NearMeButton({ query }: { query: Record<string, string | undefined> }) {
  const t = useTranslations("Venues");
  const router = useRouter();
  const [status, setStatus] = useState<"idle" | "locating" | "error">("idle");

  function handleClick() {
    if (!("geolocation" in navigator)) {
      setStatus("error");
      return;
    }

    setStatus("locating");
    navigator.geolocation.getCurrentPosition(
      (position) => {
        router.push(
          buildFilterHref("/venues", query, {
            lat: String(position.coords.latitude),
            lng: String(position.coords.longitude),
          }),
        );
      },
      () => setStatus("error"),
      { timeout: 10_000 },
    );
  }

  return (
    <div className="flex flex-col gap-1">
      <button
        type="button"
        onClick={handleClick}
        disabled={status === "locating"}
        className="inline-flex items-center gap-2 rounded-full border border-brand-border bg-background px-4 py-2 text-sm font-medium text-foreground transition-colors duration-200 hover:border-brand-primary/40 disabled:opacity-60 cursor-pointer"
      >
        <MapPinLine size={16} weight="bold" />
        {status === "locating" ? t("locating") : t("nearMe")}
      </button>
      {status === "error" && <p className="text-xs text-red-600">{t("geoDenied")}</p>}
    </div>
  );
}
