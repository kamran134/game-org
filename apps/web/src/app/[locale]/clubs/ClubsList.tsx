"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Users, Lock } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { getClubsAuthed, type ClubKind, type ClubListItem } from "@/lib/clubsApi";

// "Мои" — тот же приём, что EventsList: клиентский дозапрос с cookie,
// потому что сама страница списка SSR-анонимна (Шаг 21).
export function ClubsList({
  apiUrl,
  cityId,
  sportId,
  kind,
  initialClubs,
}: {
  apiUrl: string;
  cityId?: string;
  sportId?: string;
  kind: ClubKind;
  initialClubs: ClubListItem[];
}) {
  const t = useTranslations("Clubs");
  const locale = useLocale();

  const [onlyMine, setOnlyMine] = useState(false);
  const [clubs, setClubs] = useState(initialClubs);
  const [loading, setLoading] = useState(false);

  async function toggleMine() {
    const next = !onlyMine;
    setOnlyMine(next);
    if (!next) {
      setClubs(initialClubs);
      return;
    }
    setLoading(true);
    try {
      setClubs(await getClubsAuthed(apiUrl, locale, { cityId, sportId, kind, onlyMine: true }));
    } catch {
      setClubs([]);
    } finally {
      setLoading(false);
    }
  }

  const emptyText = kind === "Group" ? t("emptyGroups") : t("empty");
  const emptyMineText = kind === "Group" ? t("emptyMineGroups") : t("emptyMine");

  return (
    <>
      <div className="mb-6 flex items-center gap-2 text-sm font-medium">
        <button
          type="button"
          onClick={toggleMine}
          className={`cursor-pointer rounded-full border px-4 py-1.5 transition-colors duration-200 ${
            onlyMine
              ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
              : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
          }`}
        >
          {t("onlyMine")}
        </button>
      </div>

      {loading ? (
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
