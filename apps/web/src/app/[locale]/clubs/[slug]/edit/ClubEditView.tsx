"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getClubAuthed, type ClubDetail } from "@/lib/clubsApi";
import { ClubForm } from "../../ClubForm";
import { ClubAvatarUploader } from "./ClubAvatarUploader";

type Option = { id: string; name: string };

export function ClubEditView({
  apiUrl,
  locale,
  slug,
  cities,
  sports,
}: {
  apiUrl: string;
  locale: string;
  slug: string;
  cities: Option[];
  sports: Option[];
}) {
  const t = useTranslations("Clubs");
  const [club, setClub] = useState<ClubDetail | null | undefined>(undefined);

  useEffect(() => {
    getClubAuthed(apiUrl, locale, slug)
      .then(setClub)
      .catch(() => setClub(null));
  }, [apiUrl, locale, slug]);

  if (club === undefined) return null;
  if (!club) return <p className="text-foreground/70">{t("notFound")}</p>;
  if (club.viewerRole !== "Owner" && club.viewerRole !== "Admin") return <p className="text-red-600">{t("noAccessEdit")}</p>;

  return (
    <div className="flex flex-col gap-4">
      <ClubAvatarUploader apiUrl={apiUrl} club={club} />
      <ClubForm apiUrl={apiUrl} cities={cities} sports={sports} mode="edit" club={club} kind={club.kind} />
    </div>
  );
}
