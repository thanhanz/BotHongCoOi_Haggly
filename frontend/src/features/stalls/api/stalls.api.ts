import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { PublicStallDetails } from "./stalls.contracts";

export function getPublicStallDetails(
  stallId: string,
  options: ApiRequestOptions = {},
): Promise<PublicStallDetails> {
  return apiRequest<PublicStallDetails>({
    method: "GET",
    url: `/stalls/${encodeURIComponent(stallId)}`,
    ...options,
  });
}
