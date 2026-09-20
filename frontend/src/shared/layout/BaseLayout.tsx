import type { ReactNode } from "react";
import { Footer } from "./Footer";
import { Header } from "./Header";

export interface BaseLayoutProps {
  children: ReactNode;
}

export function BaseLayout({ children }: BaseLayoutProps) {
  return (
    <div dir="ltr" className="flex min-h-dvh flex-col">
      <div className="bg-brand-primary px-4 py-2 text-center font-data text-xs font-semibold text-white">Đi chợ truyền thống · Tươi ngon mỗi ngày</div>
      <Header />
      <main className="w-full min-w-0 flex-1 bg-surface-canvas">{children}</main>
      <Footer />
    </div>
  );
}
