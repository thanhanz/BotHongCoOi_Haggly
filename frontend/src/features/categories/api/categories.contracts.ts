export const CATALOG_STATUSES = {
  ACTIVE: 0,
  INACTIVE: 1,
  DRAFT: 2,
} as const;

export type CatalogStatus = (typeof CATALOG_STATUSES)[keyof typeof CATALOG_STATUSES];

export interface Category {
  id: string;
  parentCategoryId: string | null;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
  displayOrder: number;
  status: CatalogStatus;
}

export interface CategoryListParams {
  stallId?: string;
  page?: number;
  pageSize?: number;
}
