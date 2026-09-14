import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { Cart, CreatedOrder, CreateOrderRequest, UpdateCartItemRequest } from "./cart.contracts";

export function getCart(options: ApiRequestOptions): Promise<Cart> {
  return apiRequest<Cart>({ method: "GET", url: "/cart", ...options });
}

export function updateCartItem(cartItemId: string, request: UpdateCartItemRequest, options: ApiRequestOptions): Promise<Cart> {
  return apiRequest<Cart, UpdateCartItemRequest>({
    method: "PUT",
    url: `/cart/items/${encodeURIComponent(cartItemId)}`,
    data: request,
    ...options,
  });
}

export function removeCartItem(cartItemId: string, options: ApiRequestOptions): Promise<Cart> {
  return apiRequest<Cart>({ method: "DELETE", url: `/cart/items/${encodeURIComponent(cartItemId)}`, ...options });
}

export function clearCart(options: ApiRequestOptions): Promise<Cart> {
  return apiRequest<Cart>({ method: "DELETE", url: "/cart", ...options });
}

export function createOrder(request: CreateOrderRequest, options: ApiRequestOptions): Promise<CreatedOrder> {
  return apiRequest<CreatedOrder, CreateOrderRequest>({ method: "POST", url: "/orders", data: request, ...options });
}
