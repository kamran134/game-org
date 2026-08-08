import Link from "next/link";
import { Inter, Oswald } from "next/font/google";
import {
  ArrowRight,
  MapPinLine,
  ShieldCheck,
  Trophy,
  UserCircle,
} from "@phosphor-icons/react/dist/ssr";

const brandHeading = Oswald({
  variable: "--font-brand-heading",
  weight: ["600", "700"],
  subsets: ["latin", "cyrillic"],
});

const brandSans = Inter({
  variable: "--font-brand-sans",
  weight: ["400", "500", "600"],
  subsets: ["latin", "cyrillic"],
});

const features = [
  {
    icon: UserCircle,
    title: "Карточка игрока",
    description:
      "Один профиль на все виды спорта: уровень, позиции, история игр — виден сразу тем, кого ты приглашаешь.",
  },
  {
    icon: MapPinLine,
    title: "Площадки рядом",
    description:
      "Поля, залы и корты с гео-поиском — находи, где играть, не переписываясь в десяти чатах.",
  },
  {
    icon: Trophy,
    title: "Игры и тренировки",
    description:
      "Публичные и закрытые события с записью участников, гостями и списком ожидания.",
  },
  {
    icon: ShieldCheck,
    title: "Рейтинг и надёжность",
    description:
      "Glicko-2 рейтинг по каждому виду спорта и репутация за то, что не срываешь игры в последний момент.",
  },
] as const;

const steps = [
  {
    step: "1",
    title: "Входишь через Telegram",
    description: "Без новых паролей и форм регистрации — один тап в уже знакомом приложении.",
  },
  {
    step: "2",
    title: "Заполняешь профиль",
    description: "Выбираешь виды спорта, уровень и город — это займёт меньше минуты.",
  },
  {
    step: "3",
    title: "Находишь игру",
    description: "Смотришь ближайшие площадки и события, записываешься или создаёшь своё.",
  },
] as const;

export default function Home() {
  return (
    <div className={`${brandHeading.variable} ${brandSans.variable} flex flex-1 flex-col`} style={{ fontFamily: "var(--font-brand-sans)" }}>
      {/* Hero */}
      <section className="bg-brand-background dark:bg-brand-background">
        <div className="mx-auto flex max-w-5xl flex-col items-start gap-8 px-6 py-24 sm:py-32">
          <span className="rounded-full border border-brand-border px-4 py-1.5 text-sm font-medium text-brand-foreground">
            game.org.az — спортивная соц.сеть для Азербайджана
          </span>
          <h1
            className="max-w-3xl text-5xl font-semibold leading-[1.05] tracking-tight text-brand-foreground sm:text-7xl"
            style={{ fontFamily: "var(--font-brand-heading)" }}
          >
            Найди игру.
            <br />
            Собери команду.
          </h1>
          <p className="max-w-xl text-lg leading-8 text-brand-foreground/80 sm:text-xl">
            Карточка игрока сразу для нескольких видов спорта, площадки с гео-поиском и игры —
            публичные и для своих. Без чатов с потерянными сообщениями.
          </p>
          <div className="flex flex-col gap-4 sm:flex-row">
            <Link
              href="/login"
              className="group inline-flex h-14 items-center justify-center gap-2 rounded-full bg-brand-accent px-8 text-base font-semibold text-brand-accent-foreground transition-colors duration-200 hover:bg-brand-accent/90 cursor-pointer"
            >
              Войти через Telegram
              <ArrowRight
                size={20}
                weight="bold"
                className="transition-transform duration-200 group-hover:translate-x-1"
              />
            </Link>
            <Link
              href="/sports"
              className="inline-flex h-14 items-center justify-center rounded-full border border-brand-border px-8 text-base font-semibold text-brand-foreground transition-colors duration-200 hover:bg-brand-muted cursor-pointer"
            >
              Смотреть виды спорта
            </Link>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="bg-background">
        <div className="mx-auto max-w-5xl px-6 py-24">
          <h2
            className="mb-12 text-3xl font-semibold tracking-tight text-foreground sm:text-4xl"
            style={{ fontFamily: "var(--font-brand-heading)" }}
          >
            Что внутри
          </h2>
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            {features.map(({ icon: Icon, title, description }) => (
              <div
                key={title}
                className="group rounded-2xl border border-black/10 p-8 transition-colors duration-200 hover:border-brand-primary/40 hover:bg-brand-background dark:border-white/10 dark:hover:bg-brand-background/10"
              >
                <div className="mb-5 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-primary/10 text-brand-primary">
                  <Icon size={26} weight="bold" />
                </div>
                <h3 className="mb-2 text-xl font-semibold text-foreground">{title}</h3>
                <p className="text-base leading-7 text-foreground/70">{description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="bg-brand-muted/40 dark:bg-brand-background/20">
        <div className="mx-auto max-w-5xl px-6 py-24">
          <h2
            className="mb-12 text-3xl font-semibold tracking-tight text-brand-foreground sm:text-4xl"
            style={{ fontFamily: "var(--font-brand-heading)" }}
          >
            Как это работает
          </h2>
          <div className="grid grid-cols-1 gap-10 sm:grid-cols-3">
            {steps.map(({ step, title, description }) => (
              <div key={step}>
                <div
                  className="mb-4 text-5xl font-bold text-brand-primary/30"
                  style={{ fontFamily: "var(--font-brand-heading)" }}
                >
                  {step}
                </div>
                <h3 className="mb-2 text-lg font-semibold text-brand-foreground">{title}</h3>
                <p className="text-base leading-7 text-brand-foreground/70">{description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Footer CTA */}
      <section className="bg-brand-primary">
        <div className="mx-auto flex max-w-5xl flex-col items-start gap-6 px-6 py-20 sm:flex-row sm:items-center sm:justify-between">
          <h2
            className="max-w-md text-3xl font-semibold leading-tight text-brand-primary-foreground sm:text-4xl"
            style={{ fontFamily: "var(--font-brand-heading)" }}
          >
            Готов сыграть?
          </h2>
          <Link
            href="/login"
            className="inline-flex h-14 shrink-0 items-center justify-center gap-2 rounded-full bg-brand-accent px-8 text-base font-semibold text-brand-accent-foreground transition-colors duration-200 hover:bg-brand-accent/90 cursor-pointer"
          >
            Войти через Telegram
            <ArrowRight size={20} weight="bold" />
          </Link>
        </div>
      </section>
    </div>
  );
}
