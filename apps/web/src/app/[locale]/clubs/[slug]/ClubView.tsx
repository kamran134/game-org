"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Users, MapPinLine, Copy } from "@phosphor-icons/react";
import { Link, useRouter } from "@/i18n/navigation";
import { FollowButton } from "@/components/FollowButton";
import {
  deleteClub,
  getClubAuthed,
  joinClub,
  leaveClub,
  type ClubDetail,
} from "@/lib/clubsApi";

export function ClubView({ apiUrl, locale, slug }: { apiUrl: string; locale: string; slug: string }) {
  const t = useTranslations("Clubs");
  const router = useRouter();

  const [club, setClub] = useState<ClubDetail | null | undefined>(undefined);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getClubAuthed(apiUrl, locale, slug)
      .then(setClub)
      .catch(() => setClub(null));
  }, [apiUrl, locale, slug]);

  if (club === undefined) return null;
  if (!club) {
    return (
      <main className="flex flex-1 flex-col items-center justify-center bg-brand-background px-6 py-16">
        <p className="text-foreground/70">{t("notFound")}</p>
      </main>
    );
  }

  const isManager = club.viewerRole === "Owner" || club.viewerRole === "Admin";
  const isMember = club.viewerStatus === "Active";

  async function handleJoin() {
    setBusy(true);
    setError(null);
    try {
      await joinClub(apiUrl, locale, club!.id);
      const updated = await getClubAuthed(apiUrl, locale, slug);
      setClub(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("actionError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleLeave() {
    setBusy(true);
    setError(null);
    try {
      await leaveClub(apiUrl, locale, club!.id);
      const updated = await getClubAuthed(apiUrl, locale, slug);
      setClub(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("actionError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm(t("deleteConfirm"))) return;
    setBusy(true);
    setError(null);
    try {
      await deleteClub(apiUrl, locale, club!.id);
      router.push("/clubs");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("actionError"));
      setBusy(false);
    }
  }

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto flex max-w-3xl flex-col gap-6">
        <Link
          href={club.kind === "Group" ? "/groups" : "/clubs"}
          className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground"
        >
          ← {club.kind === "Group" ? t("backToListGroups") : t("backToList")}
        </Link>

        <div className="rounded-2xl border border-brand-border bg-background p-8">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-start gap-4">
              {club.avatarUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={club.avatarUrl} alt="" className="h-16 w-16 shrink-0 rounded-2xl object-cover" />
              )}
              <div>
                <div className="flex items-center gap-2">
                  <h1 className="font-heading text-3xl font-semibold text-foreground">{club.name}</h1>
                  {club.kind === "Group" && (
                    <span className="rounded-full bg-brand-muted px-2.5 py-0.5 text-xs font-medium text-foreground/60">{t("groupBadge")}</span>
                  )}
                </div>
              <p className="mt-2 flex items-center gap-1.5 text-sm text-foreground/60">
                <Users size={16} weight="bold" />
                {t("membersCount", { count: club.membersCount })}
              </p>
              <p className="mt-1 text-sm text-foreground/60">{t("followersCount", { count: club.followersCount })}</p>
              {club.city && (
                <p className="mt-1 flex items-center gap-1.5 text-sm text-foreground/60">
                  <MapPinLine size={16} weight="bold" />
                  {club.city.nameI18n[locale] ?? club.city.slug}
                </p>
              )}
              </div>
            </div>
            {club.viewerStatus === "Pending" && (
              <span className="rounded-full bg-amber-500/15 px-3 py-1 text-sm font-medium text-amber-600 dark:text-amber-400">
                {t("statusPending")}
              </span>
            )}
          </div>

          {club.description && <p className="mt-4 text-foreground/80">{club.description}</p>}

          {club.sports.length > 0 && (
            <div className="mt-4 flex flex-wrap gap-2">
              {club.sports.map((s) => (
                <span key={s.id} className="rounded-full bg-brand-primary/10 px-3 py-1 text-sm font-medium text-brand-primary">
                  {s.emoji} {s.nameI18n[locale] ?? s.slug}
                </span>
              ))}
            </div>
          )}

          {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

          <div className="mt-6 flex flex-wrap items-center gap-4">
            <FollowButton
              apiUrl={apiUrl}
              targetType="Club"
              targetId={club.id}
              initialIsFollowing={club.viewerIsFollowing}
              onChange={(following) =>
                setClub((c) => (c ? { ...c, viewerIsFollowing: following, followersCount: c.followersCount + (following ? 1 : -1) } : c))
              }
            />
            {!isMember && club.viewerStatus !== "Pending" && (
              <button
                type="button"
                onClick={handleJoin}
                disabled={busy}
                className="cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
              >
                {club.visibility === "RequestOnly" ? t("requestJoin") : t("join")}
              </button>
            )}
            {isMember && club.viewerRole === "Member" && (
              <button
                type="button"
                onClick={handleLeave}
                disabled={busy}
                className="cursor-pointer text-sm font-medium text-red-600 transition-colors duration-200 hover:underline disabled:opacity-50 dark:text-red-400"
              >
                {t("leave")}
              </button>
            )}
            {isManager && (
              <>
                <Link href={`/clubs/${club.slug}/edit`} className="text-sm font-medium text-brand-primary hover:underline">
                  {t("editClub")}
                </Link>
                <Link href={`/clubs/${club.slug}/members`} className="text-sm font-medium text-brand-primary hover:underline">
                  {t("manageMembers")}
                </Link>
              </>
            )}
            {club.viewerRole === "Owner" && (
              <button
                type="button"
                onClick={handleDelete}
                disabled={busy}
                className="cursor-pointer text-sm font-medium text-red-600 transition-colors duration-200 hover:underline disabled:opacity-50 dark:text-red-400"
              >
                {t("deleteClub")}
              </button>
            )}
          </div>

          {isManager && club.inviteCode && (
            <div className="mt-6 flex items-center gap-2 rounded-xl border border-dashed border-brand-border px-4 py-3">
              <span className="text-sm text-foreground/60">{t("inviteCode")}:</span>
              <code className="text-sm font-medium text-foreground">{club.inviteCode}</code>
              <button
                type="button"
                onClick={() => navigator.clipboard.writeText(club.inviteCode!)}
                aria-label={t("copyInviteCode")}
                className="ml-auto cursor-pointer text-foreground/50 transition-colors duration-200 hover:text-foreground"
              >
                <Copy size={16} weight="bold" />
              </button>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
