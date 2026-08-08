import { ImageResponse } from "next/og";
import { BADGE_SVG_DATA_URI } from "@/lib/brandIcon";

export const size = { width: 180, height: 180 };
export const contentType = "image/png";

// iOS композитит apple-touch-icon без учёта прозрачности и сам скругляет
// углы — поэтому квадрат целиком закрашен красным (тем же, что и заливка
// бейджа), без отдельной внешней обводки, чтобы не было шва по краю круга.
export default function AppleIcon() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#dc2626",
        }}
      >
        <img src={BADGE_SVG_DATA_URI} width={150} height={150} alt="" />
      </div>
    ),
    { ...size },
  );
}
