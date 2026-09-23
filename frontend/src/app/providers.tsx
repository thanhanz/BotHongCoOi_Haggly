import type { ReactNode } from "react";
import { AuthProvider } from "@/features/identity/components";

export function Providers({ children }: { children: ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}
