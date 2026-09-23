import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { Order } from "./orders.contracts";

export function getOrderDetails(orderId: string, options: ApiRequestOptions = {}): Promise<Order> {
  return apiRequest<Order>({ method: "GET", url: `/orders/${encodeURIComponent(orderId)}`, ...options });
}
