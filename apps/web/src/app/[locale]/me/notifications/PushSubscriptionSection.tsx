"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { getVapidPublicKey, registerDevice, unregisterDevice } from "@/lib/pushApi";

// Web Push ждёt Uint8Array в applicationServerKey, VAPID-ключ приходит base64url-строкой.
function urlBase64ToUint8Array(base64: string): Uint8Array {
  const padding = "=".repeat((4 - (base64.length % 4)) % 4);
  const base64Safe = (base64 + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64Safe);
  return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)));
}

type Status = "checking" | "unsupported" | "unavailable" | "subscribed" | "unsubscribed";

export function PushSubscriptionSection({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Notifications");
  const locale = useLocale();

  const [status, setStatus] = useState<Status>("checking");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function check(): Promise<Status> {
      if (typeof window === "undefined" || !("serviceWorker" in navigator) || !("PushManager" in window)) {
        return "unsupported";
      }
      const key = await getVapidPublicKey(apiUrl);
      if (!key) return "unavailable";

      const registration = await navigator.serviceWorker.register("/sw.js");
      const subscription = await registration.pushManager.getSubscription();
      return subscription ? "subscribed" : "unsubscribed";
    }

    check()
      .then(setStatus)
      .catch(() => setStatus("unavailable"));
  }, [apiUrl]);

  async function handleEnable() {
    setBusy(true);
    setError(null);
    try {
      const key = await getVapidPublicKey(apiUrl);
      if (!key) {
        setStatus("unavailable");
        return;
      }

      const permission = await Notification.requestPermission();
      if (permission !== "granted") {
        setError(t("push.permissionDenied"));
        return;
      }

      const registration = await navigator.serviceWorker.register("/sw.js");
      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(key) as BufferSource,
      });

      await registerDevice(apiUrl, JSON.stringify(subscription), locale);
      setStatus("subscribed");
    } catch {
      setError(t("push.enableError"));
    } finally {
      setBusy(false);
    }
  }

  async function handleDisable() {
    setBusy(true);
    setError(null);
    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();
      if (subscription) {
        await unregisterDevice(apiUrl, JSON.stringify(subscription));
        await subscription.unsubscribe();
      }
      setStatus("unsubscribed");
    } catch {
      setError(t("push.disableError"));
    } finally {
      setBusy(false);
    }
  }

  if (status === "checking") return null;

  return (
    <div className="rounded-2xl border border-brand-border bg-background px-5 py-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="font-medium text-foreground">{t("push.heading")}</p>
          <p className="mt-1 text-sm text-foreground/60">
            {status === "unsupported" && t("push.unsupported")}
            {status === "unavailable" && t("push.unavailable")}
            {status === "subscribed" && t("push.subscribedHint")}
            {status === "unsubscribed" && t("push.unsubscribedHint")}
          </p>
        </div>
        {(status === "subscribed" || status === "unsubscribed") && (
          <button
            type="button"
            onClick={status === "subscribed" ? handleDisable : handleEnable}
            disabled={busy}
            className={
              status === "subscribed"
                ? "shrink-0 cursor-pointer rounded-full border border-brand-border px-4 py-2 text-sm font-semibold text-foreground transition-colors duration-200 hover:border-red-400 hover:text-red-600 disabled:opacity-50 dark:hover:text-red-400"
                : "shrink-0 cursor-pointer rounded-full bg-brand-primary px-4 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
            }
          >
            {status === "subscribed" ? t("push.disable") : t("push.enable")}
          </button>
        )}
      </div>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
    </div>
  );
}
