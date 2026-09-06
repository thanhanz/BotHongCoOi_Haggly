import { AxiosError, AxiosHeaders } from "axios";
import { describe, expect, it } from "vitest";
import { ApiError } from "./api-error";
import { apiRequest } from "./http-client";

describe("apiRequest", () => {
  it("apiRequest_SuccessEnvelope_ReturnsData", async () => {
    // Arrange
    const expected = { id: "product-1", name: "Cà chua" };

    // Act
    const result = await apiRequest<typeof expected>({
      method: "GET",
      url: "/products/product-1",
      adapter: async (config) => ({
        config,
        data: { success: true, message: "Product retrieved successfully.", data: expected },
        headers: new AxiosHeaders(),
        status: 200,
        statusText: "OK",
      }),
    });

    // Assert
    expect(result).toEqual(expected);
  });

  it("apiRequest_ProblemDetailsResponse_ThrowsStructuredApiError", async () => {
    // Arrange
    const problem = {
      status: 404,
      title: "Product not found",
      detail: "Product product-1 was not found.",
      instance: "/api/v1/products/product-1",
      traceId: "trace-123",
    };

    // Act
    const request = apiRequest({
      method: "GET",
      url: "/products/product-1",
      adapter: async (config) => {
        throw new AxiosError("Request failed", "ERR_BAD_REQUEST", config, undefined, {
          config,
          data: problem,
          headers: new AxiosHeaders(),
          status: 404,
          statusText: "Not Found",
        });
      },
    });

    // Assert
    await expect(request).rejects.toBeInstanceOf(ApiError);
    await expect(request).rejects.toMatchObject({
      name: "ApiError",
      message: problem.detail,
      status: 404,
      problem,
    });
  });
});
