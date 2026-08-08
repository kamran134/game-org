"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import {
  fetchMe,
  removeMySport,
  updateMe,
  upsertMySport,
  type MeProfile,
  type SkillLevel,
  type Visibility,
} from "@/lib/authApi";

type Option = { id: string; name: string; emoji?: string };

const LEVELS: SkillLevel[] = ["Beginner", "Amateur", "Intermediate", "Advanced", "SemiPro", "Pro"];
const VISIBILITIES: Visibility[] = ["Public", "Followers", "Private"];

export function MeEditor({ apiUrl, cities, sports }: { apiUrl: string; cities: Option[]; sports: Option[] }) {
  const t = useTranslations("Me");
  const router = useRouter();
  const [profile, setProfile] = useState<MeProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [displayName, setDisplayName] = useState("");
  const [bio, setBio] = useState("");
  const [cityId, setCityId] = useState("");
  const [visibility, setVisibility] = useState<Visibility>("Public");

  const [newSportId, setNewSportId] = useState("");
  const [newSportLevel, setNewSportLevel] = useState<SkillLevel>("Amateur");
  const [newSportVisibility, setNewSportVisibility] = useState<Visibility>("Public");

  useEffect(() => {
    fetchMe(apiUrl)
      .then((p) => {
        if (!p) {
          router.push("/login");
          return;
        }
        setProfile(p);
        setDisplayName(p.displayName);
        setBio(p.bio ?? "");
        setCityId(p.city?.id ?? "");
        setVisibility(p.profileVisibility);
      })
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")))
      .finally(() => setLoading(false));
  }, [apiUrl, router, t]);

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      const updated = await updateMe(apiUrl, {
        displayName,
        bio,
        cityId: cityId || undefined,
        profileVisibility: visibility,
      });
      setProfile(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("saveError"));
    } finally {
      setSaving(false);
    }
  }

  async function handleAddSport() {
    if (!newSportId) return;
    setError(null);
    try {
      await upsertMySport(apiUrl, newSportId, {
        level: newSportLevel,
        isPrimary: false,
        visibility: newSportVisibility,
        positionIds: [],
      });
      const updated = await fetchMe(apiUrl);
      if (updated) setProfile(updated);
      setNewSportId("");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("addSportError"));
    }
  }

  async function handleUpdateSport(sportId: string, level: SkillLevel, sportVisibility: Visibility) {
    setError(null);
    try {
      await upsertMySport(apiUrl, sportId, {
        level,
        isPrimary: profile?.sports.find((s) => s.sportId === sportId)?.isPrimary ?? false,
        visibility: sportVisibility,
        positionIds: [],
      });
      const updated = await fetchMe(apiUrl);
      if (updated) setProfile(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("updateSportError"));
    }
  }

  async function handleRemoveSport(sportId: string) {
    setError(null);
    try {
      await removeMySport(apiUrl, sportId);
      const updated = await fetchMe(apiUrl);
      if (updated) setProfile(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("removeSportError"));
    }
  }

  if (loading) return <p>{t("loading")}</p>;
  if (!profile) return null;

  const availableSports = sports.filter((s) => !profile.sports.some((us) => us.sportId === s.id));

  return (
    <div className="flex flex-col gap-8">
      {error && <p className="text-sm text-red-600">{error}</p>}

      <section className="flex flex-col gap-3">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("fields.name")}</span>
          <input
            className="rounded border border-black/10 px-3 py-2 dark:border-white/10 dark:bg-transparent"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            maxLength={80}
          />
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("fields.bio")}</span>
          <textarea
            className="rounded border border-black/10 px-3 py-2 dark:border-white/10 dark:bg-transparent"
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            maxLength={500}
            rows={3}
          />
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("fields.city")}</span>
          <select
            className="rounded border border-black/10 px-3 py-2 dark:border-white/10 dark:bg-transparent"
            value={cityId}
            onChange={(e) => setCityId(e.target.value)}
          >
            <option value="">{t("fields.cityNotSet")}</option>
            {cities.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("fields.visibility")}</span>
          <select
            className="rounded border border-black/10 px-3 py-2 dark:border-white/10 dark:bg-transparent"
            value={visibility}
            onChange={(e) => setVisibility(e.target.value as Visibility)}
          >
            {VISIBILITIES.map((v) => (
              <option key={v} value={v}>
                {t(`visibilityOptions.${v}`)}
              </option>
            ))}
          </select>
        </label>

        <button
          className="self-start rounded bg-black px-4 py-2 text-white disabled:opacity-50 dark:bg-white dark:text-black"
          onClick={handleSave}
          disabled={saving}
        >
          {saving ? t("saving") : t("save")}
        </button>
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold">{t("mySportsHeading")}</h2>

        {profile.sports.length === 0 && <p className="text-sm opacity-70">{t("noSports")}</p>}

        <ul className="flex flex-col gap-2">
          {profile.sports.map((s) => (
            <li
              key={s.sportId}
              className="flex flex-wrap items-center gap-3 rounded-lg border border-black/10 px-4 py-3 dark:border-white/10"
            >
              <span className="font-medium">
                {s.sportEmoji} {s.sportSlug}
              </span>
              <select
                className="rounded border border-black/10 px-2 py-1 dark:border-white/10 dark:bg-transparent"
                value={s.level}
                onChange={(e) => handleUpdateSport(s.sportId, e.target.value as SkillLevel, s.visibility)}
              >
                {LEVELS.map((l) => (
                  <option key={l} value={l}>
                    {t(`levels.${l}`)}
                  </option>
                ))}
              </select>
              <select
                className="rounded border border-black/10 px-2 py-1 dark:border-white/10 dark:bg-transparent"
                value={s.visibility}
                onChange={(e) => handleUpdateSport(s.sportId, s.level, e.target.value as Visibility)}
              >
                {VISIBILITIES.map((v) => (
                  <option key={v} value={v}>
                    {t(`visibilityOptions.${v}`)}
                  </option>
                ))}
              </select>
              <button className="ml-auto text-sm text-red-600" onClick={() => handleRemoveSport(s.sportId)}>
                {t("remove")}
              </button>
            </li>
          ))}
        </ul>

        {availableSports.length > 0 && (
          <div className="flex flex-wrap items-center gap-3 rounded-lg border border-black/10 px-4 py-3 dark:border-white/10">
            <select
              className="rounded border border-black/10 px-2 py-1 dark:border-white/10 dark:bg-transparent"
              value={newSportId}
              onChange={(e) => setNewSportId(e.target.value)}
            >
              <option value="">{t("addSportPlaceholder")}</option>
              {availableSports.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.emoji} {s.name}
                </option>
              ))}
            </select>
            <select
              className="rounded border border-black/10 px-2 py-1 dark:border-white/10 dark:bg-transparent"
              value={newSportLevel}
              onChange={(e) => setNewSportLevel(e.target.value as SkillLevel)}
            >
              {LEVELS.map((l) => (
                <option key={l} value={l}>
                  {t(`levels.${l}`)}
                </option>
              ))}
            </select>
            <select
              className="rounded border border-black/10 px-2 py-1 dark:border-white/10 dark:bg-transparent"
              value={newSportVisibility}
              onChange={(e) => setNewSportVisibility(e.target.value as Visibility)}
            >
              {VISIBILITIES.map((v) => (
                <option key={v} value={v}>
                  {t(`visibilityOptions.${v}`)}
                </option>
              ))}
            </select>
            <button className="text-sm font-medium" onClick={handleAddSport} disabled={!newSportId}>
              {t("add")}
            </button>
          </div>
        )}
      </section>
    </div>
  );
}
