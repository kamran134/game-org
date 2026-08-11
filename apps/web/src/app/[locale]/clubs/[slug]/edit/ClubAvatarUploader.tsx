"use client";

import { useState, type ChangeEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { attachClubAvatar, presignClubAvatar, type ClubDetail } from "@/lib/clubsApi";
import { uploadToPresignedUrl } from "@/lib/venuesApi";

export function ClubAvatarUploader({ apiUrl, club }: { apiUrl: string; club: ClubDetail }) {
  const t = useTranslations("Clubs");
  const locale = useLocale();
  const [avatarUrl, setAvatarUrl] = useState(club.avatarUrl ?? null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleUpload(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    setUploading(true);
    setError(null);
    try {
      const presigned = await presignClubAvatar(apiUrl, locale, club.id, file.type);
      await uploadToPresignedUrl(presigned.uploadUrl, file);
      await attachClubAvatar(apiUrl, locale, club.id, presigned.mediaId);
      setAvatarUrl(presigned.publicUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("avatarError"));
    } finally {
      setUploading(false);
    }
  }

  return (
    <div className="flex items-center gap-4 rounded-2xl border border-brand-border bg-background p-8">
      {avatarUrl ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={avatarUrl} alt="" className="h-16 w-16 rounded-2xl object-cover" />
      ) : (
        <div className="h-16 w-16 rounded-2xl bg-brand-muted" />
      )}
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("avatarHeading")}</span>
        <label className="cursor-pointer text-sm font-medium text-brand-primary hover:underline">
          {uploading ? t("uploading") : t("avatarUpload")}
          <input type="file" accept="image/*" className="hidden" onChange={handleUpload} disabled={uploading} />
        </label>
        {error && <p className="text-sm text-red-600">{error}</p>}
      </div>
    </div>
  );
}
