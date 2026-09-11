import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/shared/api";
import { getCategories } from "./categories.api";

vi.mock("@/shared/api", () => ({
  apiRequest: vi.fn(),
}));

describe("getCategories", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("getCategories_WithPagination_RequestsCategoryPage", async () => {
    // Arrange
    vi.mocked(apiRequest).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
    });

    // Act
    await getCategories({ page: 1, pageSize: 20 });

    // Assert
    expect(apiRequest).toHaveBeenCalledWith({
      method: "GET",
      url: "/categories",
      params: { page: 1, pageSize: 20 },
    });
  });
});
