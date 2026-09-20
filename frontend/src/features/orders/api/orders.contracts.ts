export type OrderStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7;
export type StallFulfillmentStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface Order {
  id: string;
  orderNo: string;
  buyerId: string;
  status: OrderStatus;
  totalToCharge: number;
  totalPaid: number;
  currency: string;
  placedAt: string | null;
  paymentDueAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  fulfillments: OrderFulfillment[];
}

export interface OrderFulfillment {
  id: string;
  stallId: string;
  fulfillmentNo: string;
  status: StallFulfillmentStatus;
  subtotal: number;
  finalAmount: number;
  paidAmount: number;
  preparedAt: string | null;
  readyAt: string | null;
  pickedUpAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  items: OrderItem[];
}

export interface OrderItem {
  id: string;
  inventoryItemId: string;
  productNameSnapshot: string;
  sellingUnitSnapshot: string;
  publicUnitPriceSnapshot: number;
  finalUnitPrice: number;
  finalQuantity: number;
  lineTotal: number;
  isNegotiated: boolean;
  status: 0 | 1 | 2;
  notes: string | null;
}
