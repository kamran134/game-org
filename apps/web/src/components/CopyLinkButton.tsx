"use client";

import { useState } from "react";
import { Copy, Check } from "@phosphor-icons/react";

// Только для не-публичных событий/клубов/групп (Шаг 21) — единственный
// способ поделиться ссылкой на то, что не попадает в общий каталог.
// window.location.href — сама страница уже открыта на нужном URL, строить
// путь вручную (локаль, /events против /clubs) незачем.
export function CopyLinkButton({ label, copiedLabel }: { label: string; copiedLabel: string }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    await navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <button
      type="button"
      onClick={handleCopy}
      className="inline-flex cursor-pointer items-center gap-1.5 text-sm font-medium text-brand-primary transition-colors duration-200 hover:underline"
    >
      {copied ? <Check size={16} weight="bold" /> : <Copy size={16} weight="bold" />}
      {copied ? copiedLabel : label}
    </button>
  );
}
