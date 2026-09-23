import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { MarketplaceSearchParams, MarketplaceSearchResult } from "./search.contracts";

export function searchMarketplace(
  params: MarketplaceSearchParams,
  options: ApiRequestOptions = {},
): Promise<MarketplaceSearchResult> {
  return apiRequest<MarketplaceSearchResult>({
    method: "GET",
    url: "/search",
    params,
    ...options,
  });
}
