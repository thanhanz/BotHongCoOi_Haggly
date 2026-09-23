const DEFAULT_API_ORIGIN = "http://localhost:58558";

function removeTrailingSlashes(value: string): string {
  return value.replace(/\/+$/, "");
}

export const API_ORIGIN = removeTrailingSlashes(
  process.env.NEXT_PUBLIC_API_BASE_URL ?? DEFAULT_API_ORIGIN,
);

export const API_V1_BASE_URL = `${API_ORIGIN}/api/v1`;
