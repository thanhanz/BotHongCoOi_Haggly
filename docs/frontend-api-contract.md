# Haggly Frontend API Contract

This is the frontend-facing contract for every HTTP API currently mapped by
`backend/src/Haggly.Api/Program.cs`. Frontend feature work should read this file
instead of inspecting backend implementation. Update it whenever an endpoint,
request, response, filter, authorization rule, enum, or default changes.

## Conventions

- Base prefix: `/api/v1`. Local API origin: `http://localhost:58558`.
- JSON uses camel-case fields. GUIDs and dates are strings; timestamps are
  ISO-8601 with an offset. Money and quantities are JSON numbers.
- Protected requests use `Authorization: Bearer <accessToken>`.
- Roles: `BUYER`, `VENDOR`, `MARKET_ADMIN`, `PLATFORM_ADMIN`, `DELIVERER`.
- Policies: Buyer = `BUYER`; Vendor = `VENDOR`; Admin = either Admin role;
  Catalog contributor = Vendor or either Admin role.
- Pagination defaults to `page=1`; `pageSize=20` unless stated otherwise, and
  valid page sizes are 1–100.

```ts
interface ApiResponse<T> { success: true; message: string; data: T }
interface PagedResult<T> {
  items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number;
}
interface ProblemDetails {
  status?: number; title?: string; detail?: string; instance?: string;
  traceId?: string; [extension: string]: unknown;
}
```

Failures use Problem Details, never `ApiResponse`: `400` validation, `401`
authentication, `403` role/ownership, `404` missing resource, `409`
state/concurrency conflict, and `500` unexpected failure.

## Enum values

Except for `ApprovalStatus`, JSON bodies and responses currently serialize
enums as numbers. Enum query filters accept names; send the uppercase name.

```ts
type ApprovalStatus = "PENDING" | "APPROVED" | "REJECTED" | "SUSPENDED";
enum UserStatus { ACTIVE = 0, SUSPENDED = 1, PENDING = 2 }
enum MarketStatus { ACTIVE = 0, INACTIVE = 1, SUSPENDED = 2 }
enum StallStatus { PENDING = 0, ACTIVE = 1, SUSPENDED = 2, CLOSED = 3 }
enum CatalogStatus { ACTIVE = 0, INACTIVE = 1, DRAFT = 2 }
enum ProductUnit { KG = 0, GRAM = 1, PIECE = 2, BUNCH = 3, BOX = 4, PACK = 5, LITER = 6, OTHER = 7 }
enum InventoryTransactionType { OPENING = 0, POS_SALE = 1, ONLINE_SALE = 2, ADJUSTMENT = 3, RETURN = 4, PRICE_CHANGE = 5 }
enum OrderStatus { DRAFT = 0, NEGOTIATING = 1, AGREED = 2, PAYMENT_PENDING = 3, PAID = 4, PARTIALLY_PICKED_UP = 5, COMPLETED = 6, CANCELLED = 7 }
enum StallFulfillmentStatus { DRAFT = 0, NEGOTIATING = 1, AGREED = 2, PREPARING = 3, READY = 4, PICKED_UP = 5, CANCELLED = 6 }
enum OrderItemStatus { ACTIVE = 0, CANCELLED = 1, REFUNDED = 2 }
enum PosSaleStatus { COMPLETED = 0, CANCELLED = 1 }
enum PaymentMethodCode { CASH = 0, BANK_TRANSFER = 1, MOMO = 2, ZALOPAY = 3, VNPAY = 4 }
enum PaymentStatus { PENDING = 0, PROCESSING = 1, FAILED = 2, PAID = 3 }
type SaleChannel = "ALL" | "POS" | "ONLINE";
```

## Identity and vendor administration

| Method and route | Access | Input | Success |
|---|---|---|---|
| `POST /identity/register/buyer` | Anonymous | `RegisterBuyerRequest` | `201 ApiResponse<Registration>` |
| `POST /identity/register/vendor` | Anonymous | `RegisterVendorRequest` | `201 ApiResponse<Registration>` |
| `POST /identity/login` | Anonymous | `LoginRequest` | `200 ApiResponse<Login>` |
| `GET /identity/me` | Authenticated | — | `200 ApiResponse<CurrentUser>` |
| `GET /admin/vendors` | Admin | Filters below | `200 ApiResponse<PagedResult<Vendor>>` |
| `POST /admin/vendors/{vendorId}/approve` | Admin | Path GUID | `200 ApiResponse<Vendor>` |
| `POST /admin/vendors/{vendorId}/reject` | Admin | Path GUID | `200 ApiResponse<Vendor>` |
| `POST /admin/vendors/{vendorId}/suspend` | Admin | Path GUID | `200 ApiResponse<Vendor>` |

All routes below are also relative to `/api/v1`.

```ts
interface RegisterBuyerRequest {
  email: string; phoneNumber: string; password: string; fullName: string;
}
interface RegisterVendorRequest extends RegisterBuyerRequest {
  businessName: string; businessRegistrationNo?: string | null; taxCode?: string | null;
}
interface LoginRequest { email: string; password: string }
interface Registration { userId: string; email: string; status: string; role: string }
interface Login {
  userId: string; email: string; accessToken: string; tokenType: string;
  expiresAt: string; roles: string[];
}
interface CurrentUser { userId: string; email: string | null; roles: string[] }
interface Vendor {
  userId: string; email: string; phoneNumber: string; fullName: string;
  businessName: string; businessRegistrationNo: string | null; taxCode: string | null;
  userStatus: UserStatus; approvalStatus: ApprovalStatus; approvedAt: string | null;
  approvedBy: string | null; createdAt: string; updatedAt: string | null;
  updatedBy: string | null;
}
```

Vendor filters: required `approvalStatus`; optional `search`; `page=1`;
`pageSize=20`. Registration can return `400/409`, login `400/401`, and vendor
administration `400/404/409` in addition to auth failures.

## Markets and stalls

| Method and route | Access | Input | Success |
|---|---|---|---|
| `POST /markets` | Admin | `CreateMarketRequest` | `201 ApiResponse<Market>` |
| `GET /markets` | Admin | — | `200 ApiResponse<Market[]>` |
| `GET /markets/{id}` | Admin | Path GUID | `200 ApiResponse<Market>` |
| `PUT /markets/{id}` | Admin | `UpdateMarketRequest` | `200 ApiResponse<Market>` |
| `DELETE /markets/{id}` | Admin | Path GUID | `200 ApiResponse<boolean>` |
| `POST /markets/stalls` | Admin | `CreateStallRequest` | `201 ApiResponse<Stall>` |
| `GET /markets/stalls` | Admin | — | `200 ApiResponse<Stall[]>` |
| `GET /markets/stalls/{id}` | Admin | Path GUID | `200 ApiResponse<Stall>` |
| `PUT /markets/stalls/{id}` | Admin | `UpdateStallRequest` | `200 ApiResponse<Stall>` |
| `DELETE /markets/stalls/{id}` | Admin | Path GUID | `200 ApiResponse<boolean>` |
| `GET /stalls/{id}` | Anonymous | Path GUID | `200 ApiResponse<PublicStallDetails>` |

```ts
interface CreateMarketRequest {
  code: string; name: string; address: string; latitude?: number | null;
  longitude?: number | null; openingTime?: string | null; closingTime?: string | null;
}
interface UpdateMarketRequest {
  code: string; name: string; address: string; latitude: number | null;
  longitude: number | null; openingTime: string | null; closingTime: string | null;
  status: MarketStatus;
}
interface Market {
  id: string; code: string; name: string; address: string; latitude: number | null;
  longitude: number | null; openingTime: string | null; closingTime: string | null;
  status: MarketStatus;
}
interface CreateStallRequest {
  marketId: string; vendorId: string; code: string; name: string;
  locationDescription?: string | null; phoneNumber?: string | null;
}
interface UpdateStallRequest {
  marketId: string; vendorId: string; code: string; name: string;
  locationDescription: string | null; phoneNumber: string | null; status: StallStatus;
}
interface Stall extends UpdateStallRequest { id: string }
interface PublicStallDetails {
  id: string; code: string; name: string;
  locationDescription: string | null; phoneNumber: string | null;
}
```

Admin mutations may return `400/404/409`. The public endpoint exposes only
active, non-deleted Stalls; missing or non-active Stalls return `404`.

## Catalog and storefront

| Method and route | Access | Input | Success |
|---|---|---|---|
| `POST /categories` | Catalog contributor | `CreateCategoryRequest` | `201 ApiResponse<Category>` |
| `GET /categories` | Anonymous | Filters below | `200 ApiResponse<PagedResult<Category>>` |
| `GET /categories/{id}` | Anonymous | Path GUID | `200 ApiResponse<Category>` |
| `POST /products` | Catalog contributor | `CreateProductRequest` | `201 ApiResponse<Product>` |
| `GET /products` | Anonymous | Filters below | `200 ApiResponse<PagedResult<Product>>` |
| `GET /products/{id}` | Anonymous | Path GUID | `200 ApiResponse<Product>` |
| `POST /stalls/{stallId}/products` | Vendor + owner | `CreateProductStallRequest` | `201 ApiResponse<ProductStall>` |
| `GET /stalls/{stallId}/products` | Anonymous | Pagination | `200 ApiResponse<PagedResult<ProductStall>>` |
| `GET /stalls/{stallId}/products/{id}` | Anonymous | Path GUIDs | `200 ApiResponse<ProductStall>` |
| `PATCH /stalls/{stallId}/products/{id}` | Vendor + owner | `UpdateProductStallRequest` | `200 ApiResponse<ProductStall>` |
| `GET /product-listings` | Anonymous | Storefront filters | `200 ApiResponse<PagedResult<ProductListing>>` |

```ts
interface CreateCategoryRequest {
  name: string; slug: string; description: string | null; imageUrl: string | null;
  parentCategoryId: string | null; displayOrder: number;
}
interface Category extends CreateCategoryRequest { id: string; status: CatalogStatus }
interface CreateProductRequest {
  categoryId: string; name: string; description: string | null;
  defaultUnit: ProductUnit; imageUrl: string | null;
}
interface Product extends CreateProductRequest { id: string; status: CatalogStatus }
interface CreateProductStallRequest {
  productId: string; displayName: string | null; sellingUnit: ProductUnit;
  minimumOrderQuantity: number; currentUnitPrice: number; isNegotiable: boolean;
}
interface UpdateProductStallRequest {
  displayName: string | null; sellingUnit: ProductUnit | null;
  minimumOrderQuantity: number | null; currentUnitPrice: number | null;
  isNegotiable: boolean | null; isActive: boolean | null; expectedVersion: number;
}
interface ProductStall {
  id: string; stallId: string; productId: string; displayName: string | null;
  sellingUnit: ProductUnit; minimumOrderQuantity: number; currentUnitPrice: number;
  isNegotiable: boolean; isActive: boolean; version: number;
}
interface ProductListing {
  productId: string; productStallId: string; inventoryItemId: string;
  productName: string; displayName: string | null; imageUrl: string | null;
  stallId: string; stallName: string; stallCode: string; currentUnitPrice: number;
  sellingUnit: ProductUnit; minimumOrderQuantity: number; availableQuantity: number;
  isNegotiable: boolean;
}
```

| List | Filters/defaults |
|---|---|
| `/categories` | Optional `stallId`; `page=1`; `pageSize=20`. Stall filter returns distinct active categories represented by that Stall's active products. |
| `/products` | Optional `categoryId`; `page=1`; `pageSize=20`. |
| `/stalls/{stallId}/products` | `page=1`; `pageSize=20`. |
| `/product-listings` | Optional `categoryId`, `stallId`, `sort=home`; `page=1`; `pageSize=10`. Omitted sort behaves as `home`. |

`/product-listings` contains active products from active Stalls with positive
available stock. Use it for buyer product grids. `/stalls/{stallId}/products`
is configuration and does not prove current availability.

## Vendor inventory

All routes require Vendor role and ownership of the active Stall.

| Method and route | Input | Success |
|---|---|---|
| `GET /vendor/stalls/{stallId}/inventory` | Path GUID | `200 ApiResponse<Inventory>` |
| `POST /vendor/stalls/{stallId}/inventory/items` | `AddInventoryItemRequest` | `201 ApiResponse<InventoryItem>` |
| `GET /vendor/stalls/{stallId}/inventory/items/{inventoryItemId}` | Path GUIDs | `200 ApiResponse<InventoryItem>` |
| `POST /vendor/stalls/{stallId}/inventory/adjustments` | `AdjustInventoryRequest` | `200 ApiResponse<InventoryItem>` |
| `GET /vendor/stalls/{stallId}/inventory/ledger` | Filters below | `200 ApiResponse<PagedResult<InventoryLedger>>` |

```ts
interface AddInventoryItemRequest { productStallId: string; currentQuantity: number }
interface AdjustInventoryRequest {
  inventoryItemId: string; quantityDelta: number; reason: string; expectedVersion: number;
}
interface Inventory { id: string; stallId: string; items: InventoryItem[] }
interface InventoryItem {
  id: string; inventoryId: string; productStallId: string; currentQuantity: number;
  reservedQuantity: number; availableQuantity: number; version: number;
}
interface InventoryLedger {
  id: string; inventoryItemId: string; inventoryId: string;
  transactionType: InventoryTransactionType; quantityDelta: number;
  quantityBefore: number; quantityAfter: number; referenceType: string;
  referenceId: string | null; reason: string | null; performedBy: string | null;
  occurredAt: string;
}
```

Ledger filters: optional `inventoryItemId`, optional uppercase
`transactionType`, `page=1`, `pageSize=20`. Writes use `expectedVersion`; reload
the resource after `409` rather than replaying a stale mutation.

## Buyer cart and orders

All routes require Buyer role. Buyer ID comes from the token and is never a
request field.

| Method and route | Input | Success |
|---|---|---|
| `GET /cart` | — | `200 ApiResponse<Cart>` |
| `POST /cart/items` | `AddCartItemRequest` | `200 ApiResponse<Cart>` |
| `PUT /cart/items/{cartItemId}` | `UpdateCartItemRequest` | `200 ApiResponse<Cart>` |
| `DELETE /cart/items/{cartItemId}` | Path GUID | `200 ApiResponse<Cart>` |
| `DELETE /cart` | — | `200 ApiResponse<Cart>` |
| `POST /cart/checkout` | — | `201 ApiResponse<Order>` |
| `POST /orders` | `CreateOrderRequest` | `201 ApiResponse<Order>` |
| `GET /orders` | `page=1`, `pageSize=20` | `200 ApiResponse<PagedResult<Order>>` |
| `GET /orders/{orderId}` | Path GUID | `200 ApiResponse<Order>` |
| `POST /orders/{orderId}/cancel` | `{ reason: string }` | `200 ApiResponse<Order>` |

```ts
interface AddCartItemRequest { inventoryItemId: string; quantity: number; notes: string | null }
interface UpdateCartItemRequest { quantity: number; notes: string | null }
interface CreateOrderRequest { items: CreateOrderItemRequest[] | null }
interface CreateOrderItemRequest { inventoryItemId: string; quantity: number; notes: string | null }
interface Cart { id: string; buyerId: string; itemCount: number; subtotal: number; stalls: CartStallGroup[] }
interface CartStallGroup { stall: CartStall; subtotal: number; items: CartItem[] }
interface CartStall {
  id: string; marketId: string; code: string; name: string;
  locationDescription: string | null; phoneNumber: string | null;
}
interface CartProduct {
  id: string; categoryId: string; name: string; description: string | null;
  imageUrl: string | null;
}
interface CartOffering {
  displayName: string | null; sellingUnit: ProductUnit;
  minimumOrderQuantity: number; currentUnitPrice: number; isNegotiable: boolean;
}
interface CartItem {
  cartItemId: string; inventoryItemId: string; productStallId: string; quantity: number;
  notes: string | null; product: CartProduct; offering: CartOffering;
  remainingQuantity: number; isQuantityAvailable: boolean; lineTotal: number;
}
interface Order {
  id: string; orderNo: string; buyerId: string; status: OrderStatus;
  totalToCharge: number; totalPaid: number; currency: string; placedAt: string | null;
  paymentDueAt: string | null; cancelledAt: string | null;
  cancellationReason: string | null; fulfillments: OrderFulfillment[];
}
interface OrderFulfillment {
  id: string; stallId: string; fulfillmentNo: string; status: StallFulfillmentStatus;
  subtotal: number; finalAmount: number; paidAmount: number; preparedAt: string | null;
  readyAt: string | null; pickedUpAt: string | null; cancelledAt: string | null;
  cancellationReason: string | null; items: OrderItem[];
}
interface OrderItem {
  id: string; inventoryItemId: string; productNameSnapshot: string;
  sellingUnitSnapshot: string; publicUnitPriceSnapshot: number; finalUnitPrice: number;
  finalQuantity: number; lineTotal: number; isNegotiated: boolean;
  status: OrderItemStatus; notes: string | null;
}
```

Cart/order mutations can return `400/404/409` for invalid quantity, unavailable
stock, invalid state, or concurrent change.

## Vendor POS

All routes require Vendor role and ownership of the active Stall.

| Method and route | Input | Success |
|---|---|---|
| `POST /vendor/stalls/{stallId}/pos-sales` | `CompletePosSaleRequest` | `201 ApiResponse<PosSale>` |
| `GET /vendor/stalls/{stallId}/pos-sales` | `page=1`, `pageSize=20` | `200 ApiResponse<PagedResult<PosSale>>` |
| `GET /vendor/stalls/{stallId}/pos-sales/{posSaleId}` | Path GUIDs | `200 ApiResponse<PosSale>` |

```ts
interface CompletePosSaleRequest {
  clientRequestId: string; items: CompletePosSaleItemRequest[];
  paymentMethod?: PaymentMethodCode; amountPaid?: number | null;
}
interface CompletePosSaleItemRequest {
  inventoryItemId: string; quantity: number; expectedInventoryVersion: number;
  expectedProductStallVersion: number;
}
interface PosSale {
  id: string; stallId: string; saleNo: string; clientRequestId: string;
  status: PosSaleStatus; totalAmount: number; completedBy: string; completedAt: string;
  paymentMethod: PaymentMethodCode; paymentStatus: PaymentStatus;
  amountPaid: number; items: PosSaleItem[];
}
interface PosSaleItem {
  id: string; inventoryItemId: string; productNameSnapshot: string;
  sellingUnitSnapshot: ProductUnit; unitPrice: number; quantity: number; lineTotal: number;
}
```

`paymentMethod` defaults to `CASH` (0). `clientRequestId` is the idempotency key.
Version mismatches or conflicting key reuse can return `409`.

## Payments

`POST /payments` requires Buyer role, accepts `{ orderId: string }`, and returns
`202 ApiResponse<Payment>`:

```ts
interface Payment {
  id: string; orderId: string; paymentNo: string; amountDue: number;
  amountPaid: number; currency: string; status: PaymentStatus;
  initiatedAt: string; completedAt: string | null;
}
```

`202` means asynchronous processing was accepted, not that payment succeeded.
The current API has no payment GET/polling endpoint.

## Revenue reports

| Method and route | Access | Filters | Success |
|---|---|---|---|
| `GET /vendor/reports/revenue` | Vendor | Common + optional `stallId` | `200 ApiResponse<VendorRevenueReport>` |
| `GET /admin/reports/revenue` | Admin | Common + optional `marketId`, `vendorId`, `stallId` | `200 ApiResponse<AdminRevenueReport>` |

Common filters are optional ISO-8601 `from`, optional ISO-8601 `to`, and
optional `saleChannel=ALL|POS|ONLINE`. Defaults are the preceding 30 days
through now and `ALL`. Values normalize to UTC; `from <= to`; the range cannot
exceed 366 days; future bounds are invalid.

```ts
interface RevenueStallSummary { stallId: string; stallName: string; totalSales: number; netRevenue: number }
interface VendorRevenueReport { totalSales: number; netRevenue: number; stalls: RevenueStallSummary[] }
interface RevenueVendorSummary {
  vendorId: string; vendorName: string; totalSales: number; netRevenue: number;
  stalls: RevenueStallSummary[];
}
interface AdminRevenueReport { totalSales: number; netRevenue: number; vendors: RevenueVendorSummary[] }
```

## Frontend rules and maintenance

- Shared transport/envelope/error handling belongs in `frontend/src/shared/api`;
  endpoint calls and contracts belong under `src/features/<feature>/api`.
- Pass tokens explicitly. Do not make the shared client read browser storage.
- Omit absent query values; never send empty GUIDs or empty strings as filters.
- Treat `400`, `401`, `403`, `404`, and `409` as distinct UI states.
- Server prices, availability, totals, statuses, and versions are authoritative.
- Negotiation domain types exist, but no Negotiation HTTP endpoint is mapped;
  do not invent frontend requests for it.
- Development Swagger is at `/swagger`; JSON is at
  `/swagger/v1/swagger.json`. Executable behavior/OpenAPI wins if it conflicts
  with this file, and this file must then be corrected.

Whenever backend API behavior changes, update this document for methods,
routes, statuses, authorization, filters/defaults, request/response fields,
nullability, enum serialization, concurrency, and visible error behavior.
