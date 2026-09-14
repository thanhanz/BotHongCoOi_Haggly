import type { ProductUnit } from "@/features/products/api";

export interface Cart {
  id: string;
  buyerId: string;
  itemCount: number;
  subtotal: number;
  stalls: CartStallGroup[];
}

export interface CartStallGroup {
  stall: CartStall;
  subtotal: number;
  items: CartItem[];
}

export interface CartStall {
  id: string;
  marketId: string;
  code: string;
  name: string;
  locationDescription: string | null;
  phoneNumber: string | null;
}

export interface CartItem {
  cartItemId: string;
  inventoryItemId: string;
  productStallId: string;
  quantity: number;
  notes: string | null;
  product: {
    id: string;
    categoryId: string;
    name: string;
    description: string | null;
    imageUrl: string | null;
  };
  offering: {
    displayName: string | null;
    sellingUnit: ProductUnit;
    minimumOrderQuantity: number;
    currentUnitPrice: number;
    isNegotiable: boolean;
  };
  remainingQuantity: number;
  isQuantityAvailable: boolean;
  lineTotal: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
  notes: string | null;
}

export interface CreateOrderRequest {
  items: Array<{ inventoryItemId: string; quantity: number; notes: string | null }>;
}

export interface CreatedOrder {
  id: string;
  orderNo: string;
  totalToCharge: number;
}
