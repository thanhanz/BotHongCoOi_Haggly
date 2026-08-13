# Haggly Product Ideas

These ideas focus on behaviors that make Vietnamese traditional-market commerce
feel different from a conventional e-commerce marketplace. They are proposals,
not currently implemented features.

## Recommended differentiators

1. Shared online/offline inventory.
2. Structured per-stall haggling.
3. Variable-weight settlement.
4. One order with a coordinated market-trip pickup view.
5. A simple vendor “Today” dashboard.

## Market Trip

Turn a multi-stall order into one coordinated shopping journey.

```http
GET  /api/v1/orders/{orderId}/market-trip
POST /api/v1/stall-fulfillments/{fulfillmentId}/arrived
POST /api/v1/stall-fulfillments/{fulfillmentId}/pickup
```

The view can show stall locations, preparation status, pickup codes, and overall
progress. The order completes only after every active stall fulfillment is
collected.

Example: one order contains seafood from Cô Lan, vegetables from Chú Ba, and
tofu from Dì Sáu. The buyer sees three pickup stops with location, readiness,
and a short pickup code instead of managing three separate orders.

## Structured Haggling

Make bargaining faster and less ambiguous than a free-form chat conversation.

```http
POST /api/v1/negotiations/{negotiationId}/quick-offers
GET  /api/v1/negotiations/{negotiationId}/suggested-offers
POST /api/v1/negotiations/{negotiationId}/counter
```

Possible vendor rules include quantity discounts, minimum acceptable prices,
offer expiration, and Vietnamese-dong price rounding. An offer changes order
values only after explicit acceptance.

Example: a buyer offers ₫85,000/kg for 2 kg of pork listed at ₫95,000/kg. The
vendor counters with ₫90,000/kg, valid for two minutes. The order changes only
when the buyer accepts the counteroffer.

## Variable-Weight Products

Support meat, fish, vegetables, spices, and other goods whose final weight is
not known when the buyer creates the order.

```http
POST /api/v1/stall-fulfillments/{fulfillmentId}/items/{itemId}/final-weight
POST /api/v1/orders/{orderId}/confirm-weight-adjustments
```

Preserve requested quantity, actual quantity, unit, and final amount. Changes
outside the buyer’s accepted tolerance require confirmation before payment.

## Substitutions

Example: regular tomatoes sell out, so the vendor proposes cherry tomatoes at
₫65,000/kg. The buyer accepts or rejects the replacement while the original
item remains visible in the history.

Handle products that sell out during the day while online and walk-in buyers
share the same stock.

```http
POST /api/v1/stall-fulfillments/{fulfillmentId}/items/{itemId}/substitutions
POST /api/v1/substitutions/{substitutionId}/accept
POST /api/v1/substitutions/{substitutionId}/reject
```

Buyer preferences can be `DO_NOT_SUBSTITUTE`, `ASK_ME`, or
`VENDOR_CHOICE_WITHIN_LIMIT`. Never silently replace the original item; retain
the original and accepted replacement for traceability.

## “Today at the Market”

Create a daily storefront based on what is open and available now.

```http
GET /api/v1/public/markets/{marketId}/today
GET /api/v1/public/stalls/{stallId}/today
```

Show active stalls, today’s products, negotiable items, low-stock indicators,
recently added listings, preparation times, and market closing time.

## Trusted Stalls and Reordering

Reflect the relationship between regular customers and familiar vendors.

```http
POST   /api/v1/buyers/me/favorite-stalls/{stallId}
DELETE /api/v1/buyers/me/favorite-stalls/{stallId}
GET    /api/v1/buyers/me/favorite-stalls
GET    /api/v1/buyers/me/recently-bought
POST   /api/v1/orders/reorder/{previousOrderId}
```

Keep this lightweight: favorites and reorder history provide relationship value
without introducing the Phase 2 loyalty-program system.

## Human-Readable Inventory Timeline

Example timeline:

```text
06:10  Opening stock       +20 kg   Balance 20 kg
07:15  Walk-in POS sale     -2 kg   Balance 18 kg
07:22  Online reservation   -1 kg   Available 17 kg
08:03  Reservation expired  +1 kg   Available 18 kg
09:10  Manual adjustment  -0.5 kg   Balance 17.5 kg
```

Translate a raw inventory ledger into an explanation vendors can understand.

```http
GET /api/v1/vendor/products/{productId}/stock-timeline
```

Show opening stock, walk-in sales, online reservations, expired reservations,
manual adjustments, and the resulting balance with actor and timestamp.

## Vendor “Today” Dashboard

Give vendors one operational view instead of forcing them through separate
inventory, order, negotiation, and pickup screens.

```http
GET /api/v1/vendor/dashboard/today?stallId={stallId}
```

The projection can include current session status, low-stock products, new
negotiations, orders awaiting response, fulfillments being prepared, buyers
waiting for pickup, today’s sales, and estimated revenue. It must remain a
read-model projection and not become a second source of business state.

## Important flow additions

Example vendor dashboard response:

```json
{
  "sessionStatus": "OPEN",
  "lowStockProducts": 4,
  "pendingNegotiations": 2,
  "ordersAwaitingResponse": 3,
  "readyForPickup": 5,
  "salesToday": 2450000,
  "estimatedRevenue": 2180000
}
```

The main API plan should also include these missing workflow steps:

```http
POST /api/v1/orders/{orderId}/confirm
GET  /api/v1/vendor/fulfillments
GET  /api/v1/vendor/fulfillments/{fulfillmentId}
POST /api/v1/vendor/fulfillments/{fulfillmentId}/accept
POST /api/v1/vendor/fulfillments/{fulfillmentId}/reject
```

The recommended order flow is:

```text
Draft order
→ per-stall negotiation or acceptance
→ per-stall inventory reservation
→ buyer confirms final order
→ payment
→ preparation
→ pickup
```

Reservation expiration should be an internal scheduled operation. Public
reservation endpoints are useful only when a client action genuinely requires
them.

Example progression:

```text
Draft order
→ vendor counteroffer
→ buyer accepts
→ each stall reserves its own stock
→ buyer confirms the complete order
→ payment
→ preparation
→ pickup at each stall
```

## Implementation guidance

- Start with Market Trip, structured haggling, variable-weight settlement,
  substitutions, and the vendor Today dashboard.
- Keep all ownership, inventory, negotiation, payment, and fulfillment rules in
  Application/Domain use cases, not endpoints.
- Preserve requested values, negotiated values, final values, and audit events.
- Prefer projections over duplicating state.
- Keep delivery, loyalty programs, promotions, multi-market tenancy, and AI
  forecasting outside the MVP unless the product scope changes.
