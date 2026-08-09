"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { createVenue, updateVenue, type CreateVenueRequest, type VenueDetail, type VenueSurface } from "@/lib/venuesApi";
import { EMPTY_LOCALIZED_TEXT, type LocalizedText } from "@/lib/localized";
import { I18nField } from "@/components/I18nField";

type Option = { id: string; name: string };

const SURFACES: VenueSurface[] = [
  "NaturalGrass",
  "ArtificialGrass",
  "Parquet",
  "Rubber",
  "Sand",
  "Concrete",
  "Ice",
  "Water",
  "Other",
];

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

export function VenueForm({
  apiUrl,
  cities,
  sports,
  mode,
  venue,
}: {
  apiUrl: string;
  cities: Option[];
  sports: Option[];
  mode: "create" | "edit";
  venue?: VenueDetail;
}) {
  const t = useTranslations("Venues");
  const locale = useLocale();
  const router = useRouter();

  const [checking, setChecking] = useState(true);
  const [allowed, setAllowed] = useState(mode === "create");

  const [name, setName] = useState<LocalizedText>(venue?.nameI18n ?? EMPTY_LOCALIZED_TEXT);
  const [description, setDescription] = useState<LocalizedText>(venue?.descriptionI18n ?? EMPTY_LOCALIZED_TEXT);
  const [address, setAddress] = useState<LocalizedText>(venue?.addressI18n ?? EMPTY_LOCALIZED_TEXT);
  const [cityId, setCityId] = useState(venue?.city?.id ?? "");
  const [lat, setLat] = useState(venue?.lat ?? 40.4093);
  const [lng, setLng] = useState(venue?.lng ?? 49.8671);
  const [isIndoor, setIsIndoor] = useState<string>(
    venue?.isIndoor === true ? "yes" : venue?.isIndoor === false ? "no" : "unset",
  );
  const [surface, setSurface] = useState<VenueSurface | "">(venue?.surface ?? "");
  const [hasLighting, setHasLighting] = useState(venue?.hasLighting ?? false);
  const [hasShowers, setHasShowers] = useState(venue?.hasShowers ?? false);
  const [hasParking, setHasParking] = useState(venue?.hasParking ?? false);
  const [hasTribunes, setHasTribunes] = useState(venue?.hasTribunes ?? false);
  const [priceHint, setPriceHint] = useState(venue?.priceHint?.toString() ?? "");
  const [currency, setCurrency] = useState(venue?.currency ?? "AZN");
  const [phone, setPhone] = useState(venue?.phone ?? "");
  const [website, setWebsite] = useState(venue?.website ?? "");
  const [sportIds, setSportIds] = useState<string[]>(venue?.sports.map((s) => s.sport.id) ?? []);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then((me: MeProfile | null) => {
        if (!me) {
          router.push("/login");
          return;
        }
        setAllowed(mode === "create" || !venue || me.id === venue.createdById);
        setChecking(false);
      })
      .catch(() => setChecking(false));
  }, [apiUrl, locale, mode, router, venue]);

  function toggleSport(id: string) {
    setSportIds((prev) => (prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id]));
  }

  async function handleSubmit() {
    setSaving(true);
    setError(null);
    try {
      const body: CreateVenueRequest = {
        name,
        description,
        address,
        cityId: cityId || null,
        lat,
        lng,
        isIndoor: isIndoor === "unset" ? null : isIndoor === "yes",
        surface: surface || null,
        hasLighting,
        hasShowers,
        hasParking,
        hasTribunes,
        priceHint: priceHint ? Number(priceHint) : null,
        currency,
        phone: phone || null,
        website: website || null,
        sportIds,
      };

      if (mode === "create") {
        const created = await createVenue(apiUrl, locale, body);
        router.push(`/venues/${created.slug}`);
      } else if (venue) {
        await updateVenue(apiUrl, locale, venue.id, body);
        router.push(`/venues/${venue.slug}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : mode === "create" ? t("createError") : t("saveError"));
    } finally {
      setSaving(false);
    }
  }

  if (checking) return null;
  if (!allowed) return <p className="text-red-600">{t("noAccessEdit")}</p>;

  return (
    <div className="flex flex-col gap-4 rounded-2xl border border-brand-border bg-background p-8">
      <I18nField label={t("fields.name")} value={name} onChange={setName} maxLength={120} />
      <I18nField label={t("fields.description")} value={description} onChange={setDescription} multiline maxLength={2000} />
      <I18nField label={t("fields.address")} value={address} onChange={setAddress} maxLength={300} />

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

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">Lat</span>
          <input
            type="number"
            step="0.0001"
            className={fieldClass}
            value={lat}
            onChange={(e) => setLat(Number(e.target.value))}
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">Lng</span>
          <input
            type="number"
            step="0.0001"
            className={fieldClass}
            value={lng}
            onChange={(e) => setLng(Number(e.target.value))}
          />
        </label>
      </div>

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

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.isIndoor")}</span>
        <select className={fieldClass} value={isIndoor} onChange={(e) => setIsIndoor(e.target.value)}>
          <option value="unset">{t("fields.indoorOptions.unset")}</option>
          <option value="yes">{t("fields.indoorOptions.yes")}</option>
          <option value="no">{t("fields.indoorOptions.no")}</option>
        </select>
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.surface")}</span>
        <select className={fieldClass} value={surface} onChange={(e) => setSurface(e.target.value as VenueSurface | "")}>
          <option value="">{t("fields.surfaceNotSet")}</option>
          {SURFACES.map((s) => (
            <option key={s} value={s}>
              {t(`surfaceOptions.${s}`)}
            </option>
          ))}
        </select>
      </label>

      <div className="flex flex-col gap-2">
        <span className="text-sm font-medium text-foreground">{t("fields.amenities")}</span>
        <div className="flex flex-wrap gap-4 text-sm text-foreground">
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={hasLighting} onChange={(e) => setHasLighting(e.target.checked)} />
            {t("fields.hasLighting")}
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={hasShowers} onChange={(e) => setHasShowers(e.target.checked)} />
            {t("fields.hasShowers")}
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={hasParking} onChange={(e) => setHasParking(e.target.checked)} />
            {t("fields.hasParking")}
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={hasTribunes} onChange={(e) => setHasTribunes(e.target.checked)} />
            {t("fields.hasTribunes")}
          </label>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.priceHint")}</span>
          <input type="number" className={fieldClass} value={priceHint} onChange={(e) => setPriceHint(e.target.value)} />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-foreground">{t("fields.currency")}</span>
          <input className={fieldClass} value={currency} onChange={(e) => setCurrency(e.target.value)} maxLength={3} />
        </label>
      </div>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.phone")}</span>
        <input className={fieldClass} value={phone} onChange={(e) => setPhone(e.target.value)} maxLength={20} />
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("fields.website")}</span>
        <input className={fieldClass} value={website} onChange={(e) => setWebsite(e.target.value)} maxLength={200} />
      </label>

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
