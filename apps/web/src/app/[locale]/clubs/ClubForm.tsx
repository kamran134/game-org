"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe } from "@/lib/authApi";
import { createClub, updateClub, type ClubDetail, type ClubVisibility, type CreateClubRequest } from "@/lib/clubsApi";
import { EMPTY_LOCALIZED_TEXT, type LocalizedText } from "@/lib/localized";
import { I18nField } from "@/components/I18nField";

type Option = { id: string; name: string };

const VISIBILITIES: ClubVisibility[] = ["Public", "RequestOnly", "Private"];

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

export function ClubForm({
  apiUrl,
  cities,
  sports,
  mode,
  club,
}: {
  apiUrl: string;
  cities: Option[];
  sports: Option[];
  mode: "create" | "edit";
  club?: ClubDetail;
}) {
  const t = useTranslations("Clubs");
  const locale = useLocale();
  const router = useRouter();

  const [name, setName] = useState<LocalizedText>(club?.nameI18n ?? EMPTY_LOCALIZED_TEXT);
  const [description, setDescription] = useState<LocalizedText>(club?.descriptionI18n ?? EMPTY_LOCALIZED_TEXT);
  const [cityId, setCityId] = useState(club?.city?.id ?? "");
  const [visibility, setVisibility] = useState<ClubVisibility>(club?.visibility ?? "Public");
  const [sportIds, setSportIds] = useState<string[]>(club?.sports.map((s) => s.id) ?? []);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Проверка владения при редактировании — забота вызывающей Gate-обёртки
    // (она уже сделала авторизованный fetch клуба); здесь нужен только
    // редирект анонимного пользователя при создании.
    if (mode !== "create") return;
    fetchMe(apiUrl, locale)
      .then((me) => {
        if (!me) router.push("/login");
      })
      .catch(() => {});
  }, [apiUrl, locale, mode, router]);

  function toggleSport(id: string) {
    setSportIds((prev) => (prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id]));
  }

  async function handleSubmit() {
    setSaving(true);
    setError(null);
    try {
      const body: CreateClubRequest = { name, description, cityId: cityId || null, visibility, sportIds };

      if (mode === "create") {
        const created = await createClub(apiUrl, locale, body);
        router.push(`/clubs/${created.slug}`);
      } else if (club) {
        await updateClub(apiUrl, locale, club.id, body);
        router.push(`/clubs/${club.slug}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : mode === "create" ? t("createError") : t("saveError"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-4 rounded-2xl border border-brand-border bg-background p-8">
      <I18nField label={t("fields.name")} value={name} onChange={setName} maxLength={80} />
      <I18nField label={t("fields.description")} value={description} onChange={setDescription} multiline maxLength={1000} />

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.city")}</span>
        <select className={fieldClass} value={cityId} onChange={(e) => setCityId(e.target.value)}>
          <option value="">{t("fields.cityNotSet")}</option>
          {cities.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.visibility")}</span>
        <select className={fieldClass} value={visibility} onChange={(e) => setVisibility(e.target.value as ClubVisibility)}>
          {VISIBILITIES.map((v) => (
            <option key={v} value={v}>
              {t(`visibilityOptions.${v}`)}
            </option>
          ))}
        </select>
        <span className="text-xs text-foreground/50">{t(`visibilityHints.${visibility}`)}</span>
      </label>

      <div className="flex flex-col gap-2">
        <span className="text-sm font-medium text-foreground">{t("fields.sports")}</span>
        <div className="flex flex-wrap gap-2">
          {sports.map((s) => (
            <button
              key={s.id}
              type="button"
              onClick={() => toggleSport(s.id)}
              className={`cursor-pointer rounded-full border px-3 py-1.5 text-sm transition-colors duration-200 ${
                sportIds.includes(s.id)
                  ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
                  : "border-brand-border text-foreground/70 hover:border-brand-primary/40"
              }`}
            >
              {s.name}
            </button>
          ))}
        </div>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button
        type="button"
        onClick={handleSubmit}
        disabled={saving || (!name.az && !name.ru && !name.en)}
        className="self-start rounded-full bg-brand-primary px-6 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50 cursor-pointer"
      >
        {saving ? (mode === "create" ? t("creating") : t("saving")) : mode === "create" ? t("create") : t("save")}
      </button>
    </div>
  );
}
