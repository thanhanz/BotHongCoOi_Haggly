import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { RegisterBuyerRequest, Registration } from "./identity.contracts";

const IDENTITY_ENDPOINTS = {
  registerBuyer: "/identity/register/buyer",
} as const;

export function registerBuyer(
  request: RegisterBuyerRequest,
  options: ApiRequestOptions = {},
): Promise<Registration> {
  return apiRequest<Registration, RegisterBuyerRequest>({
    method: "POST",
    url: IDENTITY_ENDPOINTS.registerBuyer,
    data: request,
    ...options,
  });
}
