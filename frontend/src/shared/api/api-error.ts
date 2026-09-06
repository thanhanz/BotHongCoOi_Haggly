import axios, { type AxiosError } from "axios";
import type { ProblemDetails } from "./contracts/problem-details";

const FALLBACK_TITLE = "API request failed";

export class ApiError extends Error {
  readonly status: number | undefined;
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails, cause?: unknown) {
    super(problem.detail ?? problem.title ?? FALLBACK_TITLE, { cause });
    this.name = "ApiError";
    this.status = problem.status;
    this.problem = problem;
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<unknown>;
    const responseProblem = isProblemDetails(axiosError.response?.data)
      ? axiosError.response.data
      : undefined;
    
    const status = responseProblem?.status ?? axiosError.response?.status;

    return new ApiError(
      responseProblem ?? {
        status,
        title: FALLBACK_TITLE,
        detail: status
          ? `The API returned HTTP ${status}.`
          : "The API could not be reached.",
      },
      error,
    );
  }

  return new ApiError(
    {
      title: FALLBACK_TITLE,
      detail: error instanceof Error ? error.message : "An unexpected client error occurred.",
    },
    error,
  );
}
