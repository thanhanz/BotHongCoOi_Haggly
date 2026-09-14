"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import type { Login } from "@/features/identity/api";
import { AUTH_UNAUTHORIZED_EVENT, clearAuthSession, readAuthSession, writeAuthSession } from "@/shared/api";

interface AuthContextValue {
  session: Login | null;
  isReady: boolean;
  setSession: (session: Login) => void;
  clearSession: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function isValidSession(value: unknown): value is Login {
  if (!value || typeof value !== "object") return false;
  const session = value as Partial<Login>;
  return typeof session.accessToken === "string"
    && typeof session.expiresAt === "string"
    && typeof session.email === "string"
    && Array.isArray(session.roles)
    && new Date(session.expiresAt).getTime() > Date.now();
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [session, updateSession] = useState<Login | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    let active = true;
    let storedSession: Login | null = null;
    try {
      const stored: unknown = readAuthSession();
      if (isValidSession(stored)) storedSession = stored;
      else clearAuthSession();
    } catch {
      clearAuthSession();
    }
    queueMicrotask(() => {
      if (!active) return;
      updateSession(storedSession);
      setIsReady(true);
    });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    function handleUnauthorized() {
      updateSession(null);
      if (pathname !== "/login") {
        router.replace(`/login?returnTo=${encodeURIComponent(pathname)}`);
      }
    }
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
  }, [pathname, router]);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    isReady,
    setSession(nextSession) {
      writeAuthSession(nextSession);
      updateSession(nextSession);
    },
    clearSession() {
      clearAuthSession();
      updateSession(null);
    },
  }), [isReady, session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
