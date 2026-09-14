"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { Login } from "@/features/identity/api";

const SESSION_KEY = "haggly.buyer-session";

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
  const [session, updateSession] = useState<Login | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    let active = true;
    let storedSession: Login | null = null;
    try {
      const raw = sessionStorage.getItem(SESSION_KEY);
      const stored: unknown = raw ? JSON.parse(raw) : null;
      if (isValidSession(stored)) storedSession = stored;
      else sessionStorage.removeItem(SESSION_KEY);
    } catch {
      sessionStorage.removeItem(SESSION_KEY);
    }
    queueMicrotask(() => {
      if (!active) return;
      updateSession(storedSession);
      setIsReady(true);
    });
    return () => { active = false; };
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    isReady,
    setSession(nextSession) {
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(nextSession));
      updateSession(nextSession);
    },
    clearSession() {
      sessionStorage.removeItem(SESSION_KEY);
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
