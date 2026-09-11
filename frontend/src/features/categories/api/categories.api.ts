import { apiRequest, type ApiRequestOptions, type PagedResult } from "@/shared/api";
import type { Category, CategoryListParams } from "./categories.contracts";

const CATEGORY_ENDPOINTS = {
  root: "/categories",
} as const;

export function getCategories(
  params: CategoryListParams = {},
  options: ApiRequestOptions = {},
): Promise<PagedResult<Category>> {
  return apiRequest<PagedResult<Category>>({
    method: "GET",
    url: CATEGORY_ENDPOINTS.root,
    params,
    ...options,
  });
}
