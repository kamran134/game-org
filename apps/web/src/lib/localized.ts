// Зеркалит GameOrg.Api.Common.LocalizedTextDto — {az,ru,en} вместо голого
// Dictionary, иначе Kiota на нём сворачивает всё в additionalData-мешок
// (так сейчас выглядит nameI18n у городов).
export type LocalizedText = {
  az?: string | null;
  ru?: string | null;
  en?: string | null;
};

export const EMPTY_LOCALIZED_TEXT: LocalizedText = { az: "", ru: "", en: "" };

const FALLBACK_ORDER: Array<keyof LocalizedText> = ["az", "en", "ru"];

/**
 * Тот же фолбэк, что GameOrg.Api.Common.Localized.Resolve на бэкенде:
 * запрошенный язык → az → en → ru → любой непустой. На практике почти не
 * нужен — резолвнутую строку почти всегда уже прислал бэкенд, но держим для
 * мест, где на руках только сырой LocalizedText (например, сразу после
 * сохранения формы, до похода на сервер).
 */
export function pickLocalized(value: LocalizedText | null | undefined, locale: string): string {
  if (!value) return "";

  const direct = value[locale as keyof LocalizedText];
  if (direct) return direct;

  for (const lang of FALLBACK_ORDER) {
    const v = value[lang];
    if (v) return v;
  }

  for (const v of Object.values(value)) {
    if (v) return v;
  }

  return "";
}
