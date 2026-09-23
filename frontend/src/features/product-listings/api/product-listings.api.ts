import { apiRequest, type ApiRequestOptions, type PagedResult } from "@/shared/api";
import type { ProductListing, ProductListingParams } from "./product-listings.contracts";

export function getProductListings(
  params: ProductListingParams = {},
  options: ApiRequestOptions = {},
): Promise<PagedResult<ProductListing>> {
  return apiRequest<PagedResult<ProductListing>>({
    method: "GET",
    url: "/product-listings",
    params,
    ...options,
  });
}
