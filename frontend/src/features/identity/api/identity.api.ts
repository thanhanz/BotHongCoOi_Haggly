import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { Login, LoginRequest, RegisterBuyerRequest, Registration } from "./identity.contracts";

const IDENTITY_ENDPOINTS = {
  registerBuyer: "/identity/register/buyer",
  login: "/identity/login",
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

export function login(request: LoginRequest, options: ApiRequestOptions = {}): Promise<Login> {
  return apiRequest<Login, LoginRequest>({
    method: "POST",
    url: IDENTITY_ENDPOINTS.login,
    data: request,
    ...options,
  });
}
