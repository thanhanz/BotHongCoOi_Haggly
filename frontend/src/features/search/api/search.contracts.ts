import type { ProductListing } from "@/features/product-listings/api";
import type { PagedResult } from "@/shared/api";

export interface StallSearchResult {
  id: string;
  code: string;
  name: string;
  locationDescription: string | null;
  phoneNumber: string | null;
  availableProductCount: number;
  productPreview: ProductListing[];
}

export interface MarketplaceSearchResult {
  stalls: PagedResult<StallSearchResult>;
  products: PagedResult<ProductListing>;
}

export interface MarketplaceSearchParams {
  q: string;
  stallPage?: number;
  stallPageSize?: number;
  productPage?: number;
  productPageSize?: number;
}
