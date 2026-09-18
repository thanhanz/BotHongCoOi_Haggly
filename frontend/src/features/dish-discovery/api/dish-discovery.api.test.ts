import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/shared/api";
import { getDishProposal, searchCommonDishes } from "./dish-discovery.api";

vi.mock("@/shared/api", () => ({ apiRequest: vi.fn() }));

describe("dish discovery API", () => {
  beforeEach(() => vi.mocked(apiRequest).mockReset());

  it("searchCommonDishes_WithVietnameseName_SendsExactQuery", async () => {
    // Arrange
    vi.mocked(apiRequest).mockResolvedValue({ candidates: [] });

    // Act
    await searchCommonDishes("Bún bò Huế");

    // Assert
    expect(apiRequest).toHaveBeenCalledWith({
      method: "GET",
      url: "/common-dishes/search",
      params: { q: "Bún bò Huế" },
    });
  });

  it("getDishProposal_WithDishId_RequestsProposal", async () => {
    // Arrange
    const dishId = "59a8fcc4-2cb7-48d8-8090-bad5a50471d1";
    vi.mocked(apiRequest).mockResolvedValue({ dishId, dishName: "Bún bò Huế", ingredients: [] });

    // Act
    await getDishProposal(dishId);

    // Assert
    expect(apiRequest).toHaveBeenCalledWith({ method: "GET", url: `/common-dishes/${dishId}/proposal` });
  });
});
