"use client";

import { useState, type ChangeEvent } from "react";
import { useTranslations } from "next-intl";
import { Trash } from "@phosphor-icons/react";
import { attachVenuePhoto, presignVenuePhoto, removeVenuePhoto, uploadToPresignedUrl, type VenuePhoto } from "@/lib/venuesApi";

export function PhotoGallery({
  apiUrl,
  locale,
  venueId,
  initialPhotos,
}: {
  apiUrl: string;
  locale: string;
  venueId: string;
  initialPhotos: VenuePhoto[];
}) {
  const t = useTranslations("Venues");
  const [photos, setPhotos] = useState(initialPhotos);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleUpload(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    setUploading(true);
    setError(null);
    try {
      const presigned = await presignVenuePhoto(apiUrl, locale, venueId, file.type);
      await uploadToPresignedUrl(presigned.uploadUrl, file);
      const photo = await attachVenuePhoto(apiUrl, locale, venueId, {
        mediaId: presigned.mediaId,
        isCover: photos.length === 0,
        sizeBytes: file.size,
      });
      setPhotos((prev) => [...prev, photo]);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("photoError"));
    } finally {
      setUploading(false);
    }
  }

  async function handleRemove(photoId: string) {
    setError(null);
    try {
      await removeVenuePhoto(apiUrl, locale, venueId, photoId);
      setPhotos((prev) => prev.filter((p) => p.id !== photoId));
    } catch (err) {
      setError(err instanceof Error ? err.message : t("photoError"));
    }
  }

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("photosHeading")}</h2>
        <label className="cursor-pointer rounded-full border border-brand-border px-4 py-2 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-brand-primary/40">
          {uploading ? t("uploading") : t("addPhoto")}
          <input type="file" accept="image/*" className="hidden" onChange={handleUpload} disabled={uploading} />
        </label>
      </div>

      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}

      {photos.length > 0 && (
        <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          {photos.map((p) => (
            <div key={p.id} className="group relative aspect-square overflow-hidden rounded-xl bg-brand-muted">
              {/* Next/Image требует заранее знать домен раздачи (R2_PUBLIC_URL
                  пользователь ещё не настроил) — обычный <img>, пока домен не известен. */}
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={p.url} alt="" className="h-full w-full object-cover" />
              <button
                type="button"
                onClick={() => handleRemove(p.id)}
                className="absolute right-2 top-2 hidden h-7 w-7 cursor-pointer items-center justify-center rounded-full bg-black/60 text-white group-hover:flex"
                aria-label={t("removePhoto")}
              >
                <Trash size={14} weight="bold" />
              </button>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
