import Link from "next/link";
import { Container } from "@/shared/ui/container";
import { Divider } from "@/shared/ui/divider";
import { Typography } from "@/shared/ui/typography";

const footerGroups = [
  {
    title: "Khám phá",
    links: ["Chợ gần bạn", "Sạp nổi bật", "Sản phẩm hôm nay"],
  },
  {
    title: "Hỗ trợ",
    links: ["Trung tâm trợ giúp", "Hướng dẫn mua hàng", "Liên hệ Haggly"],
  },
] as const;

export function Footer() {
  return (
    <footer className="flex min-h-[320px] flex-col bg-brand-primary text-foreground-inverse">
      <Container  className="
        grid flex-1 content-center gap-xl py-xl
        md:grid-cols-[minmax(20rem,1.4fr)_repeat(2,minmax(10rem,0.6fr))]
        ">
        <div
          className="w-full"
          style={{ minWidth: "min(20rem, 100%)", maxWidth: "32rem" }}
        >
          <Link
            href="/"
            className="font-data text-2xl font-bold tracking-[-0.03em] text-foreground-inverse focus-visible:rounded-control focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-foreground-inverse"
          >
            Haggly
          </Link>
          <Typography
            className="mt-sm text-white/75"
            style={{ overflowWrap: "normal", wordBreak: "normal", whiteSpace: "normal" }}
          >
            Mang nhịp chợ Việt lên không gian số — gần gũi, minh bạch và thuận tiện cho cả người mua lẫn tiểu thương.
          </Typography>
        </div>

        {footerGroups.map((group) => (
          <nav key={group.title} aria-label={group.title}>
            <Typography as="h2" variant="labelLg" className="text-foreground-inverse">
              {group.title}
            </Typography>
            <ul className="mt-sm grid gap-xs">
              {group.links.map((label) => (
                <li key={label}>
                  <Link href="#" className="text-sm text-white/75 transition-colors hover:text-white">
                    {label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </Container>

      <Container>
        <Divider className="border-white/15" />
        <div className="flex flex-col gap-xs py-md text-sm text-white/65 sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} Haggly. Bản quyền được bảo lưu.</p>
          <p>Chợ Việt gần gũi, mua bán dễ dàng.</p>
        </div>
      </Container>
    </footer>
  );
}
