import type { ProductUnit } from "@/features/products/api";

export type ProductListingSort = "home";

export interface ProductListing {
  productId: string;
  productStallId: string;
  inventoryItemId: string;
  productName: string;
  displayName: string | null;
  imageUrl: string | null;
  stallId: string;
  stallName: string;
  stallCode: string;
  currentUnitPrice: number;
  sellingUnit: ProductUnit;
  minimumOrderQuantity: number;
  availableQuantity: number;
  isNegotiable: boolean;
}

export interface ProductListingParams {
  categoryId?: string;
  stallId?: string;
  sort?: ProductListingSort;
  page?: number;
  pageSize?: number;
}
