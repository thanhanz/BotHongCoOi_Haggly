import Link from "next/link";
import { buttonVariants } from "@/shared/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";

export default function NotFound() {
  return (
    <section className="flex min-h-[70vh] items-center py-xl">
      <Container className="max-w-xl">
        <Card>
          <CardHeader className="grid gap-xs">
            <Typography as="p" variant="labelLg" className="text-brand-secondary">
              404
            </Typography>
            <Typography as="h1" variant="headlineSm">
              Không tìm thấy trang
            </Typography>
          </CardHeader>
          <CardContent>
            <Typography className="text-foreground-secondary">
              Địa chỉ bạn truy cập không tồn tại hoặc trang đã được di chuyển.
            </Typography>
          </CardContent>
          <CardFooter>
            <Link href="/" className={buttonVariants()}>
              Về trang chủ
            </Link>
          </CardFooter>
        </Card>
      </Container>
    </section>
  );
}
