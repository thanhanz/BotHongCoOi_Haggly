import Link from "next/link";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";

export default function Home() {
  return (
    <main className="flex min-h-screen items-center py-2xl">
      <Container className="grid max-w-3xl gap-md">
        <Typography as="p" variant="labelLg" className="text-brand-secondary">
          Haggly
        </Typography>
        <Typography as="h1" variant="display">
          Chợ Việt gần gũi, mua bán dễ dàng.
        </Typography>
        <Typography variant="bodyLg" className="max-w-2xl text-foreground-secondary">
          Nền tảng đang được xây dựng. Bộ nền giao diện đã sẵn sàng để phát triển các hành trình mua bán.
        </Typography>
        <div>
          <Link
            href="/register"
            className="inline-flex h-10 items-center rounded-control bg-brand-primary px-md font-data text-sm font-semibold text-foreground-inverse hover:bg-brand-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary focus-visible:ring-offset-2"
          >
            Thử đăng ký người mua
          </Link>
        </div>
        {process.env.NODE_ENV === "development" && (
          <div>
            <Link
              href="/dev/design-system"
              className="inline-flex h-10 items-center rounded-control bg-brand-primary px-md font-data text-sm font-semibold text-foreground-inverse hover:bg-brand-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary focus-visible:ring-offset-2"
            >
              Xem design system
            </Link>
          </div>
        )}
      </Container>
    </main>
  );
}
