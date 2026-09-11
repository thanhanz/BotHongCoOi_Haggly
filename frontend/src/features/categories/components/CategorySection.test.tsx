import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getCategories, type Category } from "@/features/categories/api";
import { CategorySection } from "./CategorySection";

vi.mock("@/features/categories/api", () => ({
  getCategories: vi.fn(),
}));

const category: Category = {
  id: "category/rau-cu",
  parentCategoryId: null,
  name: "Rau củ",
  slug: "rau-cu",
  description: "Rau củ tươi mỗi ngày",
  imageUrl: null,
  displayOrder: 1,
  status: 0,
};

function categoryPage(items: Category[]) {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length > 0 ? 1 : 0,
  };
}

describe("CategorySection", () => {
  beforeEach(() => {
    vi.mocked(getCategories).mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it("CategorySection_WhileRequestPending_ShowsLoadingState", () => {
    // Arrange
    vi.mocked(getCategories).mockReturnValue(new Promise(() => undefined));

    // Act
    render(<CategorySection />);

    // Assert
    expect(screen.getByRole("status", { name: "Đang tải danh mục" })).toBeInTheDocument();
  });

  it("CategorySection_WithCategories_RendersProductLinks", async () => {
    // Arrange
    vi.mocked(getCategories).mockResolvedValue(categoryPage([category]));

    // Act
    render(<CategorySection />);

    // Assert
    const link = await screen.findByRole("link", { name: "Rau củ" });
    expect(link).toHaveAttribute("href", "/products?categoryId=category%2Frau-cu");
    expect(getCategories).toHaveBeenCalledWith(
      { page: 1, pageSize: 20 },
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    );
  });

  it("CategorySection_WithNoCategories_RendersEmptyState", async () => {
    // Arrange
    vi.mocked(getCategories).mockResolvedValue(categoryPage([]));

    // Act
    render(<CategorySection />);

    // Assert
    expect(await screen.findByText("Danh mục đang được cập nhật.")).toBeInTheDocument();
  });

  it("CategorySection_AfterRequestFailure_RetryLoadsCategories", async () => {
    // Arrange
    const user = userEvent.setup();
    vi.mocked(getCategories)
      .mockRejectedValueOnce(new Error("Unavailable"))
      .mockResolvedValueOnce(categoryPage([category]));

    // Act
    render(<CategorySection />);
    await user.click(await screen.findByRole("button", { name: "Thử lại" }));

    // Assert
    expect(await screen.findByRole("link", { name: "Rau củ" })).toBeInTheDocument();
    expect(getCategories).toHaveBeenCalledTimes(2);
  });
});
