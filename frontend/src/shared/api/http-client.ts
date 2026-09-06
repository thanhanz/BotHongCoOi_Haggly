import axios, {
  AxiosHeaders,
  type AxiosRequestConfig,
  type AxiosResponse,
  type RawAxiosHeaders,
} from "axios";
import { ApiError, toApiError } from "./api-error";
import { API_V1_BASE_URL } from "./config";
import type { ApiResponse } from "./contracts/api-response";

export interface ApiRequestOptions {
  accessToken?: string;
  signal?: AbortSignal;
}

export interface ApiRequestConfig<TBody = unknown>
  extends Omit<AxiosRequestConfig<TBody>, "baseURL"> {
  accessToken?: string;
}

export const apiClient = axios.create({
  baseURL: API_V1_BASE_URL,
  headers: {
    Accept: "application/json",
  },
});

export async function apiRequest<TResponse, TBody = unknown>(
  config: ApiRequestConfig<TBody>,
): Promise<TResponse> {
  const { accessToken, ...axiosConfig } = config;
  const headers = AxiosHeaders.from(
    axiosConfig.headers as AxiosHeaders | RawAxiosHeaders | undefined,
  );

  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);

  try {
    const response: AxiosResponse<ApiResponse<TResponse>> = await apiClient.request({
      ...axiosConfig,
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
    throw toApiError(error);
  }
}
