export type Theme = "light" | "dark";

// Имя куки делят сервер ([locale]/layout.tsx рендерит по ней data-theme) и
// клиент (ThemeToggle её перезаписывает). Год жизни, path=/ — чтобы тема
// была общей для всех локалей и разделов.
export const THEME_COOKIE = "theme";
export const THEME_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

export function isTheme(value: string | undefined): value is Theme {
  return value === "light" || value === "dark";
}
