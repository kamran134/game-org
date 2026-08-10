"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { createReport, type ReportReason, type ReportTargetType } from "@/lib/moderationApi";

const REASONS: ReportReason[] = ["Spam", "Abuse", "FakeProfile", "WrongInfo", "InappropriateContent", "Other"];

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-sm text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

export function ReportButton({ apiUrl, targetType, targetId }: { apiUrl: string; targetType: ReportTargetType; targetId: string }) {
  const t = useTranslations("Report");
  const locale = useLocale();

  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [comment, setComment] = useState("");
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit() {
    setSending(true);
    setError(null);
    try {
      await createReport(apiUrl, locale, { targetType, targetId, reason, comment: comment || null });
      setSent(true);
      setOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error"));
    } finally {
      setSending(false);
    }
  }

  if (sent) return <p className="text-sm text-foreground/60">{t("sent")}</p>;

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="cursor-pointer text-sm font-medium text-foreground/60 transition-colors duration-200 hover:text-foreground"
      >
        {t("button")}
      </button>
    );
  }

  return (
    <div className="flex flex-col gap-2 rounded-xl border border-brand-border bg-background p-4">
      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("reason")}</span>
        <select className={fieldClass} value={reason} onChange={(e) => setReason(e.target.value as ReportReason)}>
          {REASONS.map((r) => (
            <option key={r} value={r}>
              {t(`reasons.${r}`)}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1">
        <span className="text-sm font-medium text-foreground">{t("comment")}</span>
        <textarea className={fieldClass} rows={2} value={comment} onChange={(e) => setComment(e.target.value)} maxLength={1000} />
      </label>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3">
        <button
          type="button"
          onClick={handleSubmit}
          disabled={sending}
          className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
        >
          {sending ? t("sending") : t("submit")}
        </button>
        <button
          type="button"
          onClick={() => setOpen(false)}
          className="cursor-pointer text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground"
        >
          {t("cancel")}
        </button>
      </div>
    </div>
  );
}
