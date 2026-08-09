// Kiota-клиент здесь не используется: /api/venues/* появились уже после
// того, как стало ясно, что регенерация (`pnpm gen:api`) требует живого
// API с БД, недоступной в этой среде разработки. Анонимные GET (список,
// детальная, отзывы) вызываются отсюда же обычным fetch — их можно звать
// и из Server Component (страница передаёт apiUrl пропом, как везде в
// проекте), а не только из клиентских компонентов.

import type { LocalizedText } from "@/lib/localized";
import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type VenueSurface =
  | "NaturalGrass"
  | "ArtificialGrass"
  | "Parquet"
  | "Rubber"
  | "Sand"
  | "Concrete"
  | "Ice"
  | "Water"
  | "Other";

export type VenueListItem = {
  id: string;
  slug: string;
  name: string;
  address?: string | null;
  lat: number;
  lng: number;
  ratingAvg?: number | null;
  ratingCount: number;
  distanceMeters?: number | null;
};

export type VenueSportItem = {
  sport: {
    id: string;
    slug: string;
    nameI18n: Record<string, string>;
    emoji?: string | null;
    hasPositions: boolean;
    isTeamSport: boolean;
  };
  courts: number;
};

export type VenuePhoto = { id: string; url: string; isCover: boolean; sortOrder: number };

export type VenueStatus = "Draft" | "Published" | "Hidden" | "Merged";

export type VenueCity = { id: string; slug: string; nameI18n: Record<string, string>; lat: number; lng: number };

export type VenueDetail = {
  id: string;
  slug: string;
  name: string;
  description?: string | null;
  address?: string | null;
  city?: VenueCity | null;
  lat: number;
  lng: number;
  isIndoor?: boolean | null;
  surface?: VenueSurface | null;
  hasLighting: boolean;
  hasShowers: boolean;
  hasParking: boolean;
  hasTribunes: boolean;
  priceHint?: number | null;
  currency: string;
  phone?: string | null;
  website?: string | null;
  openingHours?: Record<string, unknown> | null;
  ratingAvg?: number | null;
  ratingCount: number;
  eventsCount: number;
  createdById?: string | null;
  status: VenueStatus;
  sports: VenueSportItem[];
  photos: VenuePhoto[];
  // Резолвнутые name/description/address выше — для страницы просмотра. Эти
  // три — сырые словари по всем языкам, только для формы редактирования.
  nameI18n: LocalizedText;
  descriptionI18n?: LocalizedText | null;
  addressI18n?: LocalizedText | null;
};

export type VenueReview = {
  id: string;
  authorId: string;
  authorDisplayName: string;
  rating: number;
  text?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CreateVenueRequest = {
  name: LocalizedText;
  description?: LocalizedText | null;
  address?: LocalizedText | null;
  cityId?: string | null;
  lat: number;
  lng: number;
  isIndoor?: boolean | null;
  surface?: VenueSurface | null;
  hasLighting?: boolean | null;
  hasShowers?: boolean | null;
  hasParking?: boolean | null;
  hasTribunes?: boolean | null;
  priceHint?: number | null;
  currency?: string | null;
  phone?: string | null;
  website?: string | null;
  sportIds?: string[] | null;
};

export type UpdateVenueRequest = Partial<CreateVenueRequest>;

export type UpsertReviewRequest = { rating: number; text?: string | null };

export type PresignPhotoResponse = { mediaId: string; uploadUrl: string; publicUrl: string };

// NEXT_PUBLIC_API_URL несёт /api на dev, но не локально — та же нормализация,
// что в authApi.ts.
function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

// Бэкенд резолвит мультиязычные поля (Venue.Name, отзывы автора, ...) по
// этому заголовку — без него всегда падает на дефолт сайта (az), даже если
// пользователь сидит на /ru. См. docs/PLAN.md, Шаг 7.5.
function localeHeaders(locale: string, extra?: Record<string, string>): Record<string, string> {
  return { "Accept-Language": locale, ...extra };
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function getVenues(
  apiUrl: string,
  locale: string,
  params: { cityId?: string; sportId?: string; lat?: number; lng?: number; radiusKm?: number },
): Promise<VenueListItem[]> {
  const query = new URLSearchParams();
  if (params.cityId) query.set("cityId", params.cityId);
  if (params.sportId) query.set("sportId", params.sportId);
  if (params.lat !== undefined) query.set("lat", String(params.lat));
  if (params.lng !== undefined) query.set("lng", String(params.lng));
  if (params.radiusKm !== undefined) query.set("radiusKm", String(params.radiusKm));

  const res = await fetch(`${apiBase(apiUrl)}/api/venues?${query.toString()}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить площадки (${res.status})`);
  return res.json();
}

export async function getVenue(apiUrl: string, locale: string, slug: string): Promise<VenueDetail | null> {
  const res = await fetch(`${apiBase(apiUrl)}/api/venues/${encodeURIComponent(slug)}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Не удалось загрузить площадку (${res.status})`);
  return res.json();
}

// Кредитная версия getVenue — с cookie. Нужна для Draft-площадки: анонимный
// getVenue (из Server Component) для неё всегда вернёт 404 даже автору/
// модератору, GetBySlugAsync на бэкенде смотрит на viewerId/viewerIsModerator
// из токена, а у SSR-фетча кук нет и никогда не будет. Клиентский компонент
// вызывает эту функцию, только если анонимный запрос уже вернул null.
export async function getVenueAuthed(apiUrl: string, locale: string, slug: string): Promise<VenueDetail | null> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${encodeURIComponent(slug)}`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) return null;
  return res.json();
}

export async function deleteVenue(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${id}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось удалить площадку (${res.status})`));
}

export async function getVenueModerationQueue(apiUrl: string, locale: string): Promise<VenueListItem[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/moderation/venues`, {
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить очередь модерации (${res.status})`);
  return res.json();
}

export async function publishVenue(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${id}/publish`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось опубликовать площадку (${res.status})`));
}

export async function hideVenue(apiUrl: string, locale: string, id: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${id}/hide`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось скрыть площадку (${res.status})`));
}

export async function getVenueReviews(
  apiUrl: string,
  locale: string,
  venueId: string,
  skip = 0,
  take = 20,
): Promise<VenueReview[]> {
  const res = await fetch(`${apiBase(apiUrl)}/api/venues/${venueId}/reviews?skip=${skip}&take=${take}`, {
    cache: "no-store",
    headers: localeHeaders(locale),
  });
  if (!res.ok) throw new Error(`Не удалось загрузить отзывы (${res.status})`);
  return res.json();
}

export async function createVenue(apiUrl: string, locale: string, body: CreateVenueRequest): Promise<VenueDetail> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось создать площадку (${res.status})`));
  return res.json();
}

export async function updateVenue(apiUrl: string, locale: string, id: string, body: UpdateVenueRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${id}`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить площадку (${res.status})`));
}

export async function presignVenuePhoto(
  apiUrl: string,
  locale: string,
  venueId: string,
  contentType: string,
): Promise<PresignPhotoResponse> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/photos/presign`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify({ contentType }),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось подготовить загрузку (${res.status})`));
  return res.json();
}

/** Льёт файл прямо в R2 по presigned URL — мимо нашего API. */
export async function uploadToPresignedUrl(uploadUrl: string, file: File): Promise<void> {
  const res = await fetch(uploadUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
  if (!res.ok) throw new Error(`Не удалось загрузить файл в хранилище (${res.status})`);
}

export async function attachVenuePhoto(
  apiUrl: string,
  locale: string,
  venueId: string,
  body: { mediaId: string; isCover?: boolean; sizeBytes?: number; width?: number; height?: number },
): Promise<VenuePhoto> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/photos`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось прикрепить фото (${res.status})`));
  return res.json();
}

export async function removeVenuePhoto(apiUrl: string, locale: string, venueId: string, photoId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/photos/${photoId}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось удалить фото (${res.status})`);
}

export async function createVenueReview(
  apiUrl: string,
  locale: string,
  venueId: string,
  body: UpsertReviewRequest,
): Promise<VenueReview> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/reviews`, {
    method: "POST",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось сохранить отзыв (${res.status})`));
  return res.json();
}

export async function updateVenueReview(
  apiUrl: string,
  locale: string,
  venueId: string,
  reviewId: string,
  body: UpsertReviewRequest,
): Promise<VenueReview> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/reviews/${reviewId}`, {
    method: "PATCH",
    credentials: "include",
    headers: localeHeaders(locale, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось обновить отзыв (${res.status})`));
  return res.json();
}

export async function removeVenueReview(apiUrl: string, locale: string, venueId: string, reviewId: string): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/venues/${venueId}/reviews/${reviewId}`, {
    method: "DELETE",
    credentials: "include",
    headers: localeHeaders(locale),
  });
  if (!res.ok && res.status !== 404) throw new Error(`Не удалось удалить отзыв (${res.status})`);
}
