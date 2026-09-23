import { apiRequest, type ApiRequestOptions } from "@/shared/api";
import type { CommonDishSearchResult, DishProposal } from "./dish-discovery.contracts";

export function searchCommonDishes(
  query: string,
  options: ApiRequestOptions = {},
): Promise<CommonDishSearchResult> {
  return apiRequest<CommonDishSearchResult>({
    method: "GET",
    url: "/common-dishes/search",
    params: { q: query },
    ...options,
  });
}

export function getDishProposal(
  dishId: string,
  options: ApiRequestOptions = {},
): Promise<DishProposal> {
  return apiRequest<DishProposal>({
    method: "GET",
    url: `/common-dishes/${dishId}/proposal`,
    ...options,
  });
}
