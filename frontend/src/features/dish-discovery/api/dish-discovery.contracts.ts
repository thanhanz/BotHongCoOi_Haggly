export type ProposalIngredientStatus =
  | "AVAILABLE"
  | "NO_PRODUCT"
  | "NO_ACTIVE_LISTING"
  | "OUT_OF_STOCK";

export interface CommonDishCandidate {
  dishId: string;
  externalId: string;
  name: string;
  category: string | null;
}

export interface CommonDishSearchResult {
  candidates: CommonDishCandidate[];
}

export interface ProposalListing {
  productId: string;
  productName: string;
  displayName: string;
  inventoryItemId: string;
  stallId: string;
  stallName: string;
  sellingUnit: string;
  minimumOrderQuantity: number;
  currentUnitPrice: number;
  availableQuantity: number;
}

export interface ProposalIngredient {
  canonicalIngredientId: string;
  code: string;
  name: string;
  status: ProposalIngredientStatus;
  selectedListing: ProposalListing | null;
  alternatives: ProposalListing[];
}

export interface DishProposal {
  dishId: string;
  dishName: string;
  ingredients: ProposalIngredient[];
}
