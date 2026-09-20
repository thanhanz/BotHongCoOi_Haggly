"use client";

import Link from "next/link";
import Form from "next/form";
import { useAuth } from "@/features/identity/components";
import { Avatar, AvatarFallback } from "@/shared/ui/avatar";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Input } from "@/shared/ui/input";

function SearchIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="size-5">
      <path
        d="m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}

function CartIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="size-5">
      <path
        d="M3 4h2l2.1 10.1a2 2 0 0 0 2 1.6h7.7a2 2 0 0 0 2-1.6L20 8H6m4 11.5h.01m6.49 0h.01"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function Header() {
  const { session, isReady, clearSession } = useAuth();

  return (
    <header className="z-40 min-h-16 shrink-0 border-b border-border-subtle bg-surface-raised/95 backdrop-blur md:h-16">
      <Container className="flex flex-wrap items-center gap-x-sm gap-y-xs py-xs md:h-full md:flex-nowrap md:gap-md md:py-0">
        <Link
          href="/"
          aria-label="Haggly - Trang chủ"
          className="shrink-0 font-data text-xl font-bold tracking-[-0.03em] text-brand-primary focus-visible:rounded-control focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary"
        >
          Haggly
        </Link>

        <Form action="/search" role="search" aria-label="Tìm kiếm toàn chợ" className="order-last flex min-w-0 flex-1 basis-full items-center gap-2xs md:order-none md:basis-auto">
          <Input
            name="q"
            type="search"
            aria-label="Tìm sản phẩm hoặc sạp"
            placeholder="Tìm sản phẩm hoặc sạp…"
            required
            containerClassName="min-w-0 flex-1"
            className="bg-surface-canvas"
          />
          <Button type="submit" size="icon" aria-label="Tìm kiếm" className="shrink-0"><SearchIcon /></Button>
        </Form>

        <nav aria-label="Điều hướng chính" className="hidden shrink-0 items-center gap-xs lg:flex">
          <Link
            href="/products"
            className="rounded-control px-xs py-2xs text-sm font-semibold text-foreground-secondary transition-colors hover:text-brand-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary"
          >
            Sản phẩm
          </Link>
          <Link
            href="/register"
            className="rounded-control px-xs py-2xs text-sm font-semibold text-foreground-secondary transition-colors hover:text-brand-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary"
          >
            Đăng ký
          </Link>
        </nav>

        <div className="ml-auto flex shrink-0 items-center gap-2xs md:ml-0">
          <Link href="/common-dishes" className="inline-flex min-h-10 items-center rounded-control px-2 text-xs font-semibold text-brand-primary hover:bg-ready-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary sm:px-3 sm:text-sm">
            Tìm món
          </Link>
          <Link href="/cart" aria-label="Giỏ hàng" className="inline-flex size-10 items-center justify-center rounded-control text-foreground-primary hover:bg-surface-sunken focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary">
            <CartIcon />
          </Link>
          {isReady && session ? (
            <Button type="button" variant="ghost" size="icon" aria-label={`Đăng xuất ${session.email}`} title={`Đăng xuất ${session.email}`} onClick={clearSession}>
              <Avatar size="sm"><AvatarFallback>{session.email.slice(0, 2).toUpperCase()}</AvatarFallback></Avatar>
            </Button>
          ) : (
            <Link href="/login" aria-label="Đăng nhập" className="inline-flex size-10 items-center justify-center rounded-control text-foreground-primary hover:bg-surface-sunken focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary">
              <Avatar size="sm"><AvatarFallback>ĐN</AvatarFallback></Avatar>
            </Link>
          )}
        </div>
      </Container>
    </header>
  );
}
