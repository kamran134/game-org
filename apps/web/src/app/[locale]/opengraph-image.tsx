import { ImageResponse } from "next/og";
import { BADGE_SVG_DATA_URI } from "@/lib/brandIcon";

export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

// Дублирует Metadata.title/description из messages/*.json — используем
// фиксированную строку, а не next-intl (генерация идёт вне React-рендера
// страницы, доставать переводы отдельным getTranslations здесь можно, но
// для трёх строк проще держать явную карту рядом с самим файлом).
const COPY: Record<string, { tagline: string }> = {
  az: { tagline: "Azərbaycanın idman sosial şəbəkəsi" },
  ru: { tagline: "Спортивная соц.сеть Азербайджана" },
  en: { tagline: "Sports Social Network of Azerbaijan" },
};

export default async function OpengraphImage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const tagline = COPY[locale]?.tagline ?? COPY.en.tagline;

  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          padding: "0 96px",
          background: "#1a0e0e",
          fontFamily: "sans-serif",
        }}
      >
        <img src={BADGE_SVG_DATA_URI} width={220} height={220} alt="" />
        <div style={{ display: "flex", flexDirection: "column", marginLeft: 56 }}>
          <div style={{ fontSize: 88, fontWeight: 800, color: "#fee2e2", letterSpacing: -1 }}>
            game.org.az
          </div>
          <div style={{ fontSize: 34, color: "#e7a9a9", marginTop: 16 }}>{tagline}</div>
        </div>
      </div>
    ),
    { ...size },
  );
}
