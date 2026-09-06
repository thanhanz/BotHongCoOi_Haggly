export const PRODUCT_UNITS = {
  KG: 0,
  GRAM: 1,
  PIECE: 2,
  BUNCH: 3,
  BOX: 4,
  PACK: 5,
  LITER: 6,
  OTHER: 7,
} as const;

export type ProductUnit = (typeof PRODUCT_UNITS)[keyof typeof PRODUCT_UNITS];

export const CATALOG_STATUSES = {
  ACTIVE: 0,
  INACTIVE: 1,
  DRAFT: 2,
} as const;

export type CatalogStatus = (typeof CATALOG_STATUSES)[keyof typeof CATALOG_STATUSES];

export interface Product {
  id: string;
  categoryId: string;
  name: string;
  description: string | null;
  defaultUnit: ProductUnit;
  imageUrl: string | null;
  status: CatalogStatus;
}

export interface ProductListParams {
  categoryId?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateProductRequest {
  categoryId: string;
  name: string;
  description?: string | null;
  defaultUnit: ProductUnit;
  imageUrl?: string | null;
}
