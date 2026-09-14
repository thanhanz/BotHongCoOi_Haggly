import axios, {
  AxiosHeaders,
  type AxiosRequestConfig,
  type AxiosResponse,
  type RawAxiosHeaders,
} from "axios";
import { ApiError, toApiError } from "./api-error";
import { API_V1_BASE_URL } from "./config";
import type { ApiResponse } from "./contracts/api-response";
import { readAuthSession, signalUnauthorized } from "./auth-session";

export interface ApiRequestOptions {
  signal?: AbortSignal;
}

export type ApiRequestConfig<TBody = unknown> = Omit<AxiosRequestConfig<TBody>, "baseURL">;

export const apiClient = axios.create({
  baseURL: API_V1_BASE_URL,
  headers: {
    Accept: "application/json",
  },
});

export async function apiRequest<TResponse, TBody = unknown>(
  config: ApiRequestConfig<TBody>,
): Promise<TResponse> {
  const accessToken = readAuthSession()?.accessToken;
  const headers = AxiosHeaders.from(
    config.headers as AxiosHeaders | RawAxiosHeaders | undefined,
  );

  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);

  try {
    const response: AxiosResponse<ApiResponse<TResponse>> = await apiClient.request({
      ...config,
      headers,
    });

    if (!response.data?.success) {
      throw new ApiError({
        status: response.status,
        title: "API response was unsuccessful",
        detail: response.data?.message,
      });
    }

    return response.data.data;
  } catch (error) {
    if (accessToken && axios.isAxiosError(error) && error.response?.status === 401) {
      signalUnauthorized();
    }
    throw toApiError(error);
  }
}
