import type { ReactNode } from "react";
import { Footer } from "./Footer";
import { Header } from "./Header";

export interface BaseLayoutProps {
  children: ReactNode;
}

export function BaseLayout({ children }: BaseLayoutProps) {
  return (
    <div className="flex h-dvh flex-col overflow-hidden">
      <Header />
      <main className="mx-[10px] min-h-0 flex-1 overflow-y-auto">{children}</main>
      <Footer />
    </div>
  );
}
