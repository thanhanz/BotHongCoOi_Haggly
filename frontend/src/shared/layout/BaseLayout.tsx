import type { ReactNode } from "react";
import { Footer } from "./Footer";
import { Header } from "./Header";

export interface BaseLayoutProps {
  children: ReactNode;
}

export function BaseLayout({ children }: BaseLayoutProps) {
  return (
    <div dir="ltr" className="flex min-h-dvh flex-col">
      <Header />
      <main className="mx-auto w-[calc(100%-20px)] max-w-[1440px] flex-1">{children}</main>
      <Footer />
    </div>
  );
}
