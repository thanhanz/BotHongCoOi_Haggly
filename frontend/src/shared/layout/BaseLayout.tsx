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
      <main className="w-full min-w-0 flex-1">{children}</main>
      <Footer />
    </div>
  );
}
