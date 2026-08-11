"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import {
  approveJoinRequest,
  getClubAuthed,
  getClubJoinRequests,
  getClubMembers,
  inviteMember,
  regenerateInviteCode,
  rejectJoinRequest,
  removeMember,
  setMemberRole,
  transferOwnership,
  type ClubDetail,
  type ClubMember,
} from "@/lib/clubsApi";

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

export function ClubMembersView({ apiUrl, locale, slug }: { apiUrl: string; locale: string; slug: string }) {
  const t = useTranslations("Clubs");

  const [club, setClub] = useState<ClubDetail | null | undefined>(undefined);
  const [members, setMembers] = useState<ClubMember[] | null>(null);
  const [requests, setRequests] = useState<ClubMember[] | null>(null);
  const [inviteHandle, setInviteHandle] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadMembers = useCallback(
    (clubId: string) => {
      getClubMembers(apiUrl, locale, clubId).then(setMembers).catch(() => setError(t("actionError")));
      getClubJoinRequests(apiUrl, locale, clubId).then(setRequests).catch(() => setRequests([]));
    },
    [apiUrl, locale, t],
  );

  useEffect(() => {
    getClubAuthed(apiUrl, locale, slug).then((c) => {
      setClub(c);
      if (c && (c.viewerRole === "Owner" || c.viewerRole === "Admin")) loadMembers(c.id);
    });
  }, [apiUrl, locale, slug, loadMembers]);

  if (club === undefined) return null;
  if (!club) return <p className="text-foreground/70">{t("notFound")}</p>;
  if (club.viewerRole !== "Owner" && club.viewerRole !== "Admin") return <p className="text-red-600">{t("noAccessEdit")}</p>;

  const isOwner = club.viewerRole === "Owner";

  async function handleInvite() {
    if (!inviteHandle.trim() || !club) return;
    setBusyId("invite");
    setError(null);
    try {
      const res = await fetch(`${apiBase(apiUrl)}/api/users/${encodeURIComponent(inviteHandle.trim())}`, {
        headers: { "Accept-Language": locale },
      });
      if (!res.ok) throw new Error(t("userNotFound"));
      const user = await res.json();
      if (!user.id) throw new Error(t("userNotFound"));

      await inviteMember(apiUrl, locale, club.id, user.id);
      setInviteHandle("");
      loadMembers(club.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleApprove(userId: string) {
    if (!club) return;
    setBusyId(userId);
    try {
      await approveJoinRequest(apiUrl, locale, club.id, userId);
      loadMembers(club.id);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleReject(userId: string) {
    if (!club) return;
    setBusyId(userId);
    try {
      await rejectJoinRequest(apiUrl, locale, club.id, userId);
      loadMembers(club.id);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRemove(userId: string) {
    if (!club || !window.confirm(t("removeMemberConfirm"))) return;
    setBusyId(userId);
    try {
      await removeMember(apiUrl, locale, club.id, userId);
      loadMembers(club.id);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleToggleAdmin(userId: string, currentRole: ClubMember["role"]) {
    if (!club) return;
    setBusyId(userId);
    try {
      await setMemberRole(apiUrl, locale, club.id, userId, currentRole === "Admin" ? "Member" : "Admin");
      loadMembers(club.id);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleTransfer(userId: string) {
    if (!club || !window.confirm(t("transferOwnershipConfirm"))) return;
    setBusyId(userId);
    try {
      await transferOwnership(apiUrl, locale, club.id, userId);
      const updated = await getClubAuthed(apiUrl, locale, slug);
      setClub(updated);
      if (updated) loadMembers(updated.id);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRegenerateCode() {
    if (!club) return;
    setBusyId("code");
    try {
      const { code } = await regenerateInviteCode(apiUrl, locale, club.id);
      setClub((prev) => (prev ? { ...prev, inviteCode: code } : prev));
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="flex flex-col gap-8">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("manageMembers")}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <section className="flex flex-col gap-3">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("inviteHeading")}</h2>
        <div className="flex gap-3">
          <input
            className="w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted"
            placeholder={t("inviteHandlePlaceholder")}
            value={inviteHandle}
            onChange={(e) => setInviteHandle(e.target.value)}
          />
          <button
            type="button"
            onClick={handleInvite}
            disabled={busyId === "invite" || !inviteHandle.trim()}
            className="shrink-0 cursor-pointer rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
          >
            {t("invite")}
          </button>
        </div>

        {club.inviteCode && (
          <div className="flex items-center gap-2 text-sm text-foreground/60">
            <span>
              {t("inviteCode")}: <code className="font-medium text-foreground">{club.inviteCode}</code>
            </span>
            <button
              type="button"
              onClick={handleRegenerateCode}
              disabled={busyId === "code"}
              className="cursor-pointer text-brand-primary hover:underline disabled:opacity-50"
            >
              {t("regenerateCode")}
            </button>
          </div>
        )}
      </section>

      {requests !== null && requests.length > 0 && (
        <section className="flex flex-col gap-3">
          <h2 className="font-heading text-lg font-semibold text-foreground">{t("joinRequestsHeading")}</h2>
          <ul className="flex flex-col gap-2">
            {requests.map((r) => (
              <li
                key={r.userId}
                className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-3"
              >
                <span className="text-foreground">{r.displayName}</span>
                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={() => handleApprove(r.userId)}
                    disabled={busyId === r.userId}
                    className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
                  >
                    {t("approve")}
                  </button>
                  <button
                    type="button"
                    onClick={() => handleReject(r.userId)}
                    disabled={busyId === r.userId}
                    className="cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
                  >
                    {t("reject")}
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="flex flex-col gap-3">
        <h2 className="font-heading text-lg font-semibold text-foreground">{t("membersHeading")}</h2>
        {members === null ? (
          <p className="text-foreground/70">{t("loading")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {members.map((m) => (
              <li
                key={m.userId}
                className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-brand-border bg-background px-5 py-3"
              >
                <div>
                  <span className="text-foreground">{m.displayName}</span>
                  <span className="ml-2 text-sm text-foreground/50">{t(`roleOptions.${m.role}`)}</span>
                </div>
                {m.role !== "Owner" && (
                  <div className="flex items-center gap-3">
                    {isOwner && (
                      <button
                        type="button"
                        onClick={() => handleToggleAdmin(m.userId, m.role)}
                        disabled={busyId === m.userId}
                        className="cursor-pointer text-sm text-brand-primary hover:underline disabled:opacity-50"
                      >
                        {m.role === "Admin" ? t("demoteToMember") : t("promoteToAdmin")}
                      </button>
                    )}
                    {isOwner && (
                      <button
                        type="button"
                        onClick={() => handleTransfer(m.userId)}
                        disabled={busyId === m.userId}
                        className="cursor-pointer text-sm text-foreground/60 hover:underline disabled:opacity-50"
                      >
                        {t("makeOwner")}
                      </button>
                    )}
                    {(isOwner || m.role === "Member") && (
                      <button
                        type="button"
                        onClick={() => handleRemove(m.userId)}
                        disabled={busyId === m.userId}
                        className="cursor-pointer text-sm text-red-600 hover:underline disabled:opacity-50 dark:text-red-400"
                      >
                        {t("removeMember")}
                      </button>
                    )}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
