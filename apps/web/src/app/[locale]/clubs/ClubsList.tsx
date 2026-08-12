"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { Users, Lock } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { getClubsAuthed, type ClubKind, type ClubListItem } from "@/lib/clubsApi";

// Тот же принцип, что в EventsList: единственный источник истины — URL
// (?mine=1). Локального состояния фильтра нет, поэтому рассинхрону между
// адресом и отрисованным списком взяться неоткуда.
export function ClubsList({
  apiUrl,
  basePath,
  cityId,
  sportId,
  kind,
  initialClubs,
}: {
  apiUrl: string;
  basePath: "/clubs" | "/groups";
  cityId?: string;
  sportId?: string;
  kind: ClubKind;
  initialClubs: ClubListItem[];
}) {
  const t = useTranslations("Clubs");
  const locale = useLocale();
  const searchParams = useSearchParams();

  const onlyMine = searchParams.get("mine") === "1";
  const filterKey = `${kind}|${cityId ?? ""}|${sportId ?? ""}`;
  const [mine, setMine] = useState<{ key: string; items: ClubListItem[] } | null>(null);

  useEffect(() => {
    if (!onlyMine) return;
    let cancelled = false;
    getClubsAuthed(apiUrl, locale, { cityId, sportId, kind, onlyMine: true })
      .then((items) => {
        if (!cancelled) setMine({ key: filterKey, items });
      })
      .catch(() => {
        if (!cancelled) setMine({ key: filterKey, items: [] });
      });
    return () => {
      cancelled = true;
    };
  }, [apiUrl, locale, cityId, sportId, kind, onlyMine, filterKey]);

  const mineReady = mine !== null && mine.key === filterKey;
  const clubs = onlyMine ? (mineReady ? mine.items : null) : initialClubs;

  function hrefWith(overrides: Record<string, string | null>): string {
    const next = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(overrides)) {
      if (value === null) next.delete(key);
      else next.set(key, value);
    }
    const qs = next.toString();
    return qs ? `${basePath}?${qs}` : basePath;
  }

  const emptyText = kind === "Group" ? t("emptyGroups") : t("empty");
  const emptyMineText = kind === "Group" ? t("emptyMineGroups") : t("emptyMine");

  return (
    <>
      <div className="mb-6 flex items-center gap-2 text-sm font-medium">
        <Link
          href={hrefWith({ mine: onlyMine ? null : "1" })}
          className={`rounded-full border px-4 py-1.5 transition-colors duration-200 ${
            onlyMine
              ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
              : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
          }`}
        >
          {t("onlyMine")}
        </Link>
      </div>

      {clubs === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : clubs.length === 0 ? (
        <p className="text-foreground/70">{onlyMine ? emptyMineText : emptyText}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {clubs.map((c) => (
            <li key={c.id}>
              <Link
                href={`/clubs/${c.slug}`}
                className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
              >
                <div className="flex items-center gap-2">
                  {c.visibility === "Private" && <Lock size={16} weight="bold" className="text-foreground/40" />}
                  <div>
                    <p className="font-medium text-foreground">{c.name}</p>
                    {c.city && <p className="mt-1 text-sm text-foreground/60">{c.city.nameI18n[locale] ?? c.city.slug}</p>}
                  </div>
                </div>
                <span className="flex shrink-0 items-center gap-1 text-sm text-foreground/50">
                  <Users size={14} weight="bold" />
                  {c.membersCount}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
