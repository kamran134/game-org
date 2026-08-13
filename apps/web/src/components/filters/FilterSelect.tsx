"use client";

// Тот же визуальный стиль, что уже использовался для селектов роли/статуса
// в /admin/users (Шаг 23) — здесь просто вынесен в переиспользуемый вид,
// раз теперь его используют три каталога (события/площадки/клубы).
export function FilterSelect({
  value,
  onChange,
  placeholder,
  options,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  options: { value: string; label: string }[];
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="rounded-full border border-brand-border bg-background px-3 py-2 text-sm text-foreground"
    >
      <option value="">{placeholder}</option>
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
