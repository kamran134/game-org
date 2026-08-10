"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { banUser, getReportQueue, resolveReport, type ReportItem } from "@/lib/moderationApi";

export function ReportsQueue({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Moderation");
  const tReport = useTranslations("Report");
  const locale = useLocale();

  const [reports, setReports] = useState<ReportItem[] | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadQueue = useCallback(() => {
    getReportQueue(apiUrl, locale)
      .then(setReports)
      .catch(() => setError(t("actionError")));
  }, [apiUrl, locale, t]);

  useEffect(() => {
    loadQueue();
  }, [loadQueue]);

  async function handleResolve(id: string, status: "Resolved" | "Rejected") {
    setBusyId(id);
    setError(null);
    try {
      await resolveReport(apiUrl, locale, id, { status });
      setReports((prev) => prev?.filter((r) => r.id !== id) ?? null);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleBan(report: ReportItem) {
    setBusyId(report.id);
    setError(null);
    try {
      await banUser(apiUrl, locale, report.targetId);
    } catch {
      setError(t("actionError"));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {error && <p className="text-sm text-red-600">{error}</p>}

      {reports === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : reports.length === 0 ? (
        <p className="text-foreground/70">{t("empty")}</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {reports.map((report) => (
            <li key={report.id} className="flex flex-col gap-2 rounded-2xl border border-brand-border bg-background px-5 py-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <span className="rounded-full bg-brand-muted px-2.5 py-0.5 text-xs font-medium text-brand-foreground">
                    {t(`targetTypes.${report.targetType}`)}
                  </span>
                  <span className="ml-2 font-medium text-foreground">{report.targetSummary}</span>
                </div>
                <span className="text-sm text-foreground/60">{tReport(`reasons.${report.reason}`)}</span>
              </div>
              {report.comment && <p className="text-sm text-foreground/70">{report.comment}</p>}
              <p className="text-sm text-foreground/50">{t("reportedBy", { name: report.reporterDisplayName })}</p>
              <div className="flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  onClick={() => handleResolve(report.id, "Resolved")}
                  disabled={busyId === report.id}
                  className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
                >
                  {t("markResolved")}
                </button>
                <button
                  type="button"
                  onClick={() => handleResolve(report.id, "Rejected")}
                  disabled={busyId === report.id}
                  className="cursor-pointer rounded-full border border-brand-border px-4 py-1.5 text-sm font-medium text-foreground/70 transition-colors duration-200 hover:text-foreground disabled:opacity-50"
                >
                  {t("markRejected")}
                </button>
                {report.targetType === "User" && (
                  <button
                    type="button"
                    onClick={() => handleBan(report)}
                    disabled={busyId === report.id}
                    className="cursor-pointer text-sm font-medium text-red-600 transition-colors duration-200 hover:underline disabled:opacity-50 dark:text-red-400"
                  >
                    {t("banUser")}
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
