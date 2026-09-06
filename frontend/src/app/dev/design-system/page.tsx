import { notFound } from "next/navigation";
import type { CSSProperties, ReactNode } from "react";
import { Avatar, AvatarFallback, AvatarImage } from "@/shared/ui/avatar";
import { Badge, type BadgeProps } from "@/shared/ui/badge";
import { Button, type ButtonProps } from "@/shared/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";
import { Checkbox } from "@/shared/ui/checkbox";
import { Container } from "@/shared/ui/container";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/shared/ui/dialog";
import { Divider } from "@/shared/ui/divider";
import { Input } from "@/shared/ui/input";
import { Radio } from "@/shared/ui/radio";
import { Select } from "@/shared/ui/select";
import { Textarea } from "@/shared/ui/textarea";
import { Typography, type TypographyProps } from "@/shared/ui/typography";

const colors = [
  ["Brand primary", "--brand-primary", "#1E513A"],
  ["Brand secondary", "--brand-secondary", "#C85A32"],
  ["Brand tertiary", "--brand-tertiary", "#E89A3C"],
  ["Canvas", "--surface-canvas", "#FBF9F5"],
  ["Surface raised", "--surface-raised", "#FFFFFF"],
  ["Surface sunken", "--surface-sunken", "#F2EDE4"],
  ["Text primary", "--text-primary", "#1C2520"],
  ["Error", "--state-error", "#BA1A1A"],
] as const;

const typeSamples: Array<[TypographyProps["variant"], string]> = [
  ["display", "Phiên chợ hôm nay"],
  ["displayMobile", "Nông sản tươi"],
  ["headlineLg", "Món ngon từ chợ Việt"],
  ["headlineLgMobile", "Gần gũi mỗi ngày"],
  ["headlineMd", "Rau củ vừa về sạp"],
  ["headlineSm", "Lựa chọn của cô Ba"],
  ["titleMd", "Cà chua Đà Lạt"],
  ["bodyLg", "Mua bán dễ dàng, trò chuyện thân tình và nhận hàng tại chợ."],
  ["bodyMd", "Sản phẩm được cập nhật theo lượng hàng thực tế tại từng sạp."],
  ["bodySm", "Cập nhật lúc 08:30 sáng nay"],
  ["labelLg", "SẠP 12B"],
  ["labelMd", "TRẠNG THÁI ĐƠN"],
  ["labelSm", "ĐƠN VỊ: KG"],
  ["priceDisplay", "125.000 ₫"],
  ["priceTabular", "24,5 kg · 08:30"],
];

const buttonVariants: NonNullable<ButtonProps["variant"]>[] = [
  "primary",
  "secondary",
  "tertiary",
  "outline",
  "ghost",
  "destructive",
  "bargain",
];

const badgeVariants: NonNullable<BadgeProps["variant"]>[] = [
  "default",
  "primary",
  "secondary",
  "tertiary",
  "neutral",
  "success",
  "warning",
  "error",
  "preparing",
  "ready",
  "awaitingPickup",
  "unit",
  "stall",
];

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="grid gap-md">
      <Typography as="h2" variant="headlineMd">{title}</Typography>
      {children}
    </section>
  );
}

function SearchIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </svg>
  );
}

export default function DesignSystemPage() {
  if (process.env.NODE_ENV !== "development") notFound();

  return (
    <main className="py-xl md:py-2xl">
      <Container className="grid gap-2xl">
        <header className="grid max-w-3xl gap-sm">
          <Badge variant="primary">Developer preview</Badge>
          <Typography as="h1" variant="display">Haggly Design System</Typography>
          <Typography variant="bodyLg" className="text-foreground-secondary">
            Nền tảng thị giác và các primitive dùng chung cho trải nghiệm người mua, tiểu thương và quản trị viên.
          </Typography>
        </header>

        <Divider />

        <Section title="Colors">
          <div className="grid gap-sm sm:grid-cols-2 lg:grid-cols-4">
            {colors.map(([name, token, value]) => (
              <Card key={token} className="overflow-hidden">
                <div className="h-24 border-b border-border-subtle" style={{ background: `var(${token})` } as CSSProperties} />
                <CardContent className="grid gap-2xs py-sm">
                  <Typography variant="labelMd">{name}</Typography>
                  <Typography variant="bodySm" className="font-data text-foreground-secondary">{value}</Typography>
                </CardContent>
              </Card>
            ))}
          </div>
        </Section>

        <Section title="Typography">
          <Card>
            <CardContent className="grid gap-lg py-md">
              {typeSamples.map(([variant, sample]) => (
                <div key={variant} className="grid gap-2xs">
                  <span className="font-data text-xs text-foreground-secondary">{variant}</span>
                  <Typography variant={variant}>{sample}</Typography>
                </div>
              ))}
            </CardContent>
          </Card>
        </Section>

        <Section title="Buttons">
          <Card>
            <CardContent className="grid gap-md py-md">
              <div className="flex flex-wrap gap-sm">
                {buttonVariants.map((variant) => (
                  <Button key={variant} variant={variant}>{variant}</Button>
                ))}
              </div>
              <div className="flex flex-wrap items-center gap-sm">
                <Button size="sm">Nhỏ</Button>
                <Button size="md">Vừa</Button>
                <Button size="lg">Lớn</Button>
                <Button size="icon" aria-label="Tìm kiếm"><SearchIcon /></Button>
                <Button loading>Đang xử lý</Button>
                <Button disabled>Không khả dụng</Button>
              </div>
            </CardContent>
          </Card>
        </Section>

        <Section title="Form controls">
          <div className="grid gap-md lg:grid-cols-2">
            <Card><CardContent className="grid gap-md py-md">
              <Input label="Tên sản phẩm" placeholder="Nhập tên sản phẩm" required />
              <Input label="Tìm trong chợ" placeholder="Rau, củ, thịt, cá…" leftIcon={<SearchIcon />} helperText="Tìm theo tên sản phẩm hoặc sạp" />
              <Input label="Số lượng" value="2 kg" disabled readOnly />
              <Input label="Tên sản phẩm" defaultValue="" error="Tên sản phẩm là bắt buộc" />
            </CardContent></Card>
            <Card><CardContent className="grid gap-md py-md">
              <Textarea label="Ghi chú cho tiểu thương" placeholder="Ví dụ: chọn giúp mình quả vừa chín…" />
              <Select label="Đơn vị" defaultValue="kg">
                <option value="kg">Kilôgam (kg)</option>
                <option value="bunch">Bó</option>
                <option value="item">Cái</option>
              </Select>
              <Select label="Quầy hàng" error="Vui lòng chọn quầy" defaultValue="">
                <option value="" disabled>Chọn quầy</option>
                <option value="12b">Sạp 12B</option>
              </Select>
            </CardContent></Card>
          </div>
        </Section>

        <Section title="Checkboxes and radios">
          <Card><CardContent className="grid gap-md py-md sm:grid-cols-2">
            <div className="grid gap-sm">
              <Checkbox label="Chọn tất cả sản phẩm" defaultChecked />
              <Checkbox label="Nhận thông báo khi có hàng" description="Chúng tôi sẽ báo khi tiểu thương cập nhật." />
              <Checkbox label="Không khả dụng" disabled />
            </div>
            <div className="grid gap-sm">
              <Radio name="pickup" label="Nhận tại sạp" defaultChecked />
              <Radio name="pickup" label="Nhận tại cổng chợ" />
              <Radio name="pickup-disabled" label="Không khả dụng" disabled />
            </div>
          </CardContent></Card>
        </Section>

        <Section title="Badges">
          <Card><CardContent className="flex flex-wrap gap-sm py-md">
            {badgeVariants.map((variant) => (
              <Badge key={variant} variant={variant}>
                {variant === "awaitingPickup" ? "Chờ nhận hàng" : variant === "ready" ? "Đã sẵn sàng" : variant === "preparing" ? "Đang chuẩn bị" : variant === "stall" ? "Sạp 12B" : variant === "unit" ? "kg" : variant}
              </Badge>
            ))}
          </CardContent></Card>
        </Section>

        <Section title="Cards">
          <div className="grid gap-md md:grid-cols-3">
            {(["raised", "sunken", "outlined"] as const).map((variant) => (
              <Card key={variant} variant={variant}>
                <CardHeader><Typography as="h3" variant="titleMd">{variant}</Typography></CardHeader>
                <CardContent><Typography variant="bodyMd" className="text-foreground-secondary">Một vùng chứa trung tính để feature có thể ghép nội dung.</Typography></CardContent>
                <CardFooter><Button variant="outline" size="sm">Xem thêm</Button></CardFooter>
              </Card>
            ))}
          </div>
        </Section>

        <Section title="Avatars and dialog">
          <Card><CardContent className="flex flex-wrap items-center gap-md py-md">
            <Avatar size="sm"><AvatarFallback>AN</AvatarFallback></Avatar>
            <Avatar size="md"><AvatarFallback>CB</AvatarFallback></Avatar>
            <Avatar size="lg">
              <AvatarImage src="/avatar-demo.jpg" alt="Ảnh đại diện của cô Ba" />
              <AvatarFallback>CB</AvatarFallback>
            </Avatar>
            <Dialog>
              <DialogTrigger asChild><Button>Mở hộp thoại</Button></DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Xác nhận thao tác</DialogTitle>
                  <DialogDescription>Đây là dialog dùng chung. Luồng nghiệp vụ sẽ được ghép tại feature sở hữu.</DialogDescription>
                </DialogHeader>
                <DialogFooter>
                  <DialogClose asChild><Button variant="outline">Hủy</Button></DialogClose>
                  <DialogClose asChild><Button>Xác nhận</Button></DialogClose>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </CardContent></Card>
        </Section>
      </Container>
    </main>
  );
}
