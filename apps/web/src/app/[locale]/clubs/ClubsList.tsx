"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Users, Lock } from "@phosphor-icons/react";
import { Link, useRouter } from "@/i18n/navigation";
import { buildFilterHref } from "@/lib/filterHref";
import { FilterSelect } from "@/components/filters/FilterSelect";
import { FilterPill } from "@/components/filters/FilterPill";
import { getClubsAuthed, type ClubKind, type ClubListItem } from "@/lib/clubsApi";

// Тот же принцип, что в EventsList: единственный источник истины — URL
// (?mine=1&sportId=..&cityId=..), значения приходят пропами со страницы.
// Локального состояния фильтра нет, поэтому рассинхрону между адресом
// и списком взяться неоткуда.
export function ClubsList({
  apiUrl,
  basePath,
  cityId,
  sportId,
  kind,
  onlyMine,
  query,
  initialClubs,
  sports,
  cities,
}: {
  apiUrl: string;
  basePath: "/clubs" | "/groups";
  cityId?: string;
  sportId?: string;
  kind: ClubKind;
  onlyMine: boolean;
  query: Record<string, string | undefined>;
  initialClubs: ClubListItem[];
  sports: { id: string; name: string; emoji?: string | null }[];
  cities: { id: string; name: string }[];
}) {
  const t = useTranslations("Clubs");
  const locale = useLocale();
  const router = useRouter();
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
    return buildFilterHref(basePath, query, overrides);
  }

  const emptyText = kind === "Group" ? t("emptyGroups") : t("empty");
  const emptyMineText = kind === "Group" ? t("emptyMineGroups") : t("emptyMine");
  const hasExtraFilters = !!(sportId || cityId);

  return (
    <>
      <div className="mb-4 flex items-center gap-2 text-sm font-medium">
        <FilterPill href={hrefWith({ mine: onlyMine ? null : "1" })} active={onlyMine}>
          {t("onlyMine")}
        </FilterPill>
      </div>

      <div className="mb-6 flex flex-wrap items-center gap-2">
        <FilterSelect
          value={sportId ?? ""}
          onChange={(v) => router.push(hrefWith({ sportId: v || null }))}
          placeholder={t("filters.sportAll")}
          options={sports.map((s) => ({ value: s.id, label: `${s.emoji ?? ""} ${s.name}`.trim() }))}
        />
        <FilterSelect
          value={cityId ?? ""}
          onChange={(v) => router.push(hrefWith({ cityId: v || null }))}
          placeholder={t("filters.cityAll")}
          options={cities.map((c) => ({ value: c.id, label: c.name }))}
        />
      </div>

      {clubs === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : clubs.length === 0 ? (
        hasExtraFilters ? (
          <div className="flex flex-col items-start gap-2">
            <p className="text-foreground/70">{t("emptyFiltered")}</p>
            <Link href={basePath} className="text-sm font-medium text-brand-primary hover:underline">
              {t("resetFilters")}
            </Link>
          </div>
        ) : (
          <p className="text-foreground/70">{onlyMine ? emptyMineText : emptyText}</p>
        )
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
