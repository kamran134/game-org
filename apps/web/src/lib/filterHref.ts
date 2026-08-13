// Единая логика для всех списков с фильтрами в URL (Шаг 21/24): берём
// текущий набор query-параметров, накатываем поверх изменения, null убирает
// параметр. Раньше эта функция была продублирована в EventsList/ClubsList/
// UsersSection почти дословно — здесь одна версия для всех.
export function buildFilterHref(basePath: string, current: Record<string, string | undefined>, overrides: Record<string, string | null>): string {
  const next = new URLSearchParams();
  for (const [key, value] of Object.entries(current)) {
    if (value !== undefined) next.set(key, value);
  }
  for (const [key, value] of Object.entries(overrides)) {
    if (value === null) next.delete(key);
    else next.set(key, value);
  }
  const qs = next.toString();
  return qs ? `${basePath}?${qs}` : basePath;
}
