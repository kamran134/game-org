"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Star } from "@phosphor-icons/react";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { createVenueReview, removeVenueReview, updateVenueReview, type VenueReview } from "@/lib/venuesApi";

export function ReviewsSection({
  apiUrl,
  venueId,
  initialReviews,
}: {
  apiUrl: string;
  venueId: string;
  initialReviews: VenueReview[];
}) {
  const t = useTranslations("Venues");
  const [reviews, setReviews] = useState(initialReviews);
  const [me, setMe] = useState<MeProfile | null>(null);
  const [rating, setRating] = useState(5);
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    fetchMe(apiUrl)
      .then(setMe)
      .catch(() => {});
  }, [apiUrl]);

  const myReview = me ? reviews.find((r) => r.authorId === me.id) : undefined;

  useEffect(() => {
    if (myReview) {
      setRating(myReview.rating);
      setText(myReview.text ?? "");
    }
  }, [myReview]);

  async function handleSubmit() {
    setSaving(true);
    setError(null);
    try {
      const result = myReview
        ? await updateVenueReview(apiUrl, venueId, myReview.id, { rating, text: text || null })
        : await createVenueReview(apiUrl, venueId, { rating, text: text || null });
      setReviews((prev) => [result, ...prev.filter((r) => r.id !== result.id)]);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("reviewError"));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!myReview) return;
    setError(null);
    try {
      await removeVenueReview(apiUrl, venueId, myReview.id);
      setReviews((prev) => prev.filter((r) => r.id !== myReview.id));
      setRating(5);
      setText("");
    } catch (err) {
      setError(err instanceof Error ? err.message : t("reviewError"));
    }
  }

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <h2 className="font-heading text-lg font-semibold text-foreground">{t("reviewsHeading")}</h2>

      {reviews.length === 0 && <p className="mt-3 text-sm text-foreground/60">{t("noReviews")}</p>}

      <ul className="mt-4 flex flex-col gap-4">
        {reviews.map((r) => (
          <li key={r.id} className="border-b border-brand-border pb-4 last:border-0 last:pb-0">
            <div className="flex items-center justify-between">
              <span className="font-medium text-foreground">{r.authorDisplayName}</span>
              <span className="flex items-center gap-1 text-brand-accent">
                <Star size={14} weight="fill" />
                {r.rating}
              </span>
            </div>
            {r.text && <p className="mt-1 text-sm text-foreground/70">{r.text}</p>}
          </li>
        ))}
      </ul>

      {me && (
        <div className="mt-6 flex flex-col gap-3 border-t border-brand-border pt-6">
          <span className="text-sm font-medium text-foreground">{t("yourRating")}</span>
          <div className="flex gap-1">
            {[1, 2, 3, 4, 5].map((n) => (
              <button
                key={n}
                type="button"
                onClick={() => setRating(n)}
                className="cursor-pointer text-brand-accent"
                aria-label={String(n)}
              >
                <Star size={22} weight={n <= rating ? "fill" : "regular"} />
              </button>
            ))}
          </div>
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={t("yourReview")}
            rows={3}
            maxLength={2000}
            className="w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted"
          />
          {error && <p className="text-sm text-red-600">{error}</p>}
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleSubmit}
              disabled={saving}
              className="self-start rounded-full bg-brand-primary px-5 py-2 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50 cursor-pointer"
            >
              {myReview ? t("editReview") : t("submitReview")}
            </button>
            {myReview && (
              <button
                type="button"
                onClick={handleDelete}
                className="cursor-pointer text-sm text-red-600 transition-colors duration-200 hover:text-red-700"
              >
                {t("deleteReview")}
              </button>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
