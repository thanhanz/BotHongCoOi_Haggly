import {
  apiRequest,
  type ApiRequestOptions,
  type PagedResult,
} from "@/shared/api";
import type {
  CreateProductRequest,
  Product,
  ProductListParams,
} from "./products.contracts";

const PRODUCT_ENDPOINTS = {
  root: "/products",
  byId: (id: string) => `/products/${encodeURIComponent(id)}`,
} as const;

export function getProducts(
  params: ProductListParams = {},
  options: ApiRequestOptions = {},
): Promise<PagedResult<Product>> {
  return apiRequest<PagedResult<Product>>({
    method: "GET",
    url: PRODUCT_ENDPOINTS.root,
    params,
    ...options,
  });
}

export function getProduct(
  id: string,
  options: ApiRequestOptions = {},
): Promise<Product> {
  return apiRequest<Product>({
    method: "GET",
    url: PRODUCT_ENDPOINTS.byId(id),
    ...options,
  });
}

export function createProduct(
  request: CreateProductRequest,
  options: ApiRequestOptions,
): Promise<Product> {
  return apiRequest<Product, CreateProductRequest>({
    method: "POST",
    url: PRODUCT_ENDPOINTS.root,
    data: request,
    ...options,
  });
}
