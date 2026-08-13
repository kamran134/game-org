"use client";

import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { buildFilterHref } from "@/lib/filterHref";
import { FilterSelect } from "@/components/filters/FilterSelect";
import { NearMeButton } from "./NearMeButton";

export function VenuesFilterBar({
  sportId,
  cityId,
  isIndoor,
  query,
  sports,
  cities,
}: {
  sportId?: string;
  cityId?: string;
  isIndoor?: string;
  query: Record<string, string | undefined>;
  sports: { id: string; name: string; emoji?: string | null }[];
  cities: { id: string; name: string }[];
}) {
  const t = useTranslations("Venues");
  const router = useRouter();

  function hrefWith(overrides: Record<string, string | null>): string {
    return buildFilterHref("/venues", query, overrides);
  }

  return (
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
      <FilterSelect
        value={isIndoor ?? ""}
        onChange={(v) => router.push(hrefWith({ isIndoor: v || null }))}
        placeholder={t("filters.indoorAll")}
        options={[
          { value: "true", label: t("fields.indoorOptions.yes") },
          { value: "false", label: t("fields.indoorOptions.no") },
        ]}
      />
      <NearMeButton query={query} />
    </div>
  );
}
