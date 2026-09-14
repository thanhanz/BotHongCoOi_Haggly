export const AUTH_SESSION_KEY = "haggly.buyer-session";
export const AUTH_UNAUTHORIZED_EVENT = "haggly:unauthorized";

export interface StoredAuthSession {
  userId: string;
  email: string;
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  roles: string[];
}

export function readAuthSession(): StoredAuthSession | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = sessionStorage.getItem(AUTH_SESSION_KEY);
    if (!raw) return null;
    const value = JSON.parse(raw) as Partial<StoredAuthSession>;
    if (typeof value.accessToken !== "string"
      || typeof value.expiresAt !== "string"
      || typeof value.email !== "string"
      || !Array.isArray(value.roles)
      || new Date(value.expiresAt).getTime() <= Date.now()) {
      clearAuthSession();
      return null;
    }
    return value as StoredAuthSession;
  } catch {
    clearAuthSession();
    return null;
  }
}

export function writeAuthSession(session: StoredAuthSession): void {
  sessionStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session));
}

export function clearAuthSession(): void {
  if (typeof window !== "undefined") sessionStorage.removeItem(AUTH_SESSION_KEY);
}

export function signalUnauthorized(): void {
  clearAuthSession();
  window.dispatchEvent(new Event(AUTH_UNAUTHORIZED_EVENT));
}
