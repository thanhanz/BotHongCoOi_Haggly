# Payments module guide

This guide records the currently executable asynchronous payment workflow.
Payment initiation, simulated provider processing, the success fan-out, and
per-stall payment allocation are implemented. Downstream `PaymentFailed`
handling is deferred to the next implementation session.

## Responsibilities

Payments owns collection state, provider attempts, payment status, per-stall
allocation, and the payment integration events. Sales owns Order and
StallFulfillment financial state, Finance owns recognized revenue, and
Inventory owns quantity and stock-ledger mutations.

The current MVP supports one complete Payment per Order. Partial payment,
refund processing, real provider webhooks, and an explicit retry-payment API
are not implemented.

## Payment processing

`POST /api/v1/payments` atomically creates a pending Payment and appends a V1
`PaymentRequested` event to the PostgreSQL outbox. The hosted outbox processor
publishes that event to RabbitMQ.

`ProcessPaymentRequestedHandler` performs the following workflow:

1. Load a `PENDING` Payment and move it to `PROCESSING`.
2. Create a pending PaymentTransaction.
3. Invoke `IPaymentProvider`.
4. On success, mark the transaction `SUCCEEDED`, mark the Payment `PAID`, create
   one PaymentAllocation per active StallFulfillment, and append
   `PaymentSucceeded` to the outbox.
5. On provider decline, mark the transaction and Payment `FAILED` and append
   `PaymentFailed` to the outbox.

The Payment, PaymentTransaction, allocations, and result event are committed
in one PostgreSQL transaction. Runtime timestamps are normalized to UTC.

The MassTransit adapter consumes from durable queue
`haggly-payments-payment-requested-v1`, bound to
`payments.payment-requested.v1`. Technical exceptions retry after 1, 5, and 15
seconds. A provider decline is a business result and is not retried by
MassTransit.

## PaymentSucceeded fan-out

The outbox publishes `PaymentSucceeded` once to
`payments.payment-succeeded.v1`. RabbitMQ copies it to three independent durable
queues, so the modules can run concurrently and retry independently:

| Module | Queue | Application handler | Persisted effect |
|---|---|---|---|
| Finance | `haggly-finance-payment-succeeded-v1` | `FinancePaymentSucceededHandler` | Append one RevenueLedger sale per PaymentAllocation. |
| Inventory | `haggly-inventory-payment-succeeded-v1` | `InventoryPaymentSucceededHandler` | Deduct active OrderItem quantities and append `ONLINE_SALE` InventoryLedger rows. |
| Sales/Order | `haggly-order-payment-succeeded-v1` | `OrderPaymentSucceededHandler` | Set `Order.TotalPaid`, move the Order to `PAID`, and apply each allocation to `StallFulfillment.PaidAmount`. |

Each Infrastructure consumer implements MassTransit
`IConsumer<PaymentSucceededEvent>` and delegates to its Application
`IEventHandler<PaymentSucceededEvent>`. Fulfillments remain `AGREED` after
payment; vendor preparation is a separate Sales workflow.

### Idempotency

- Finance skips allocations already represented in RevenueLedger and has a
  database uniqueness constraint for the allocation/entry-type pair.
- Inventory detects an existing online-sale reference and has a filtered unique
  InventoryLedger constraint per InventoryItem and payment transaction.
- Order treats an exact repeat of the fully paid allocation state as a no-op and
  rejects conflicting payment data.

A general InboxMessages table is not implemented. Consumers must continue to
assume at-least-once delivery and must not rely on processing order between
modules.

## Failure workflow: next session

`PaymentFailed` is currently persisted to the outbox and published through the
registered `payments.payment-failed.v1` exchange, but there are no downstream
module queues or handlers for it yet.

The next session should decide and implement the failure reactions explicitly:

- Define which Sales/Order state, if any, changes after the overall attempt
  fails.
- Define whether Inventory reservations remain active, expire, or are released;
  persisted reservation creation is itself still deferred.
- Keep Finance unchanged because a failed collection recognizes no revenue.
- Add module-owned queues, MassTransit consumers, idempotent handlers, retry
  policies, and focused failure tests only for the selected reactions.

Do not treat MassTransit technical retries or its automatic `_error` transport
as the business `PaymentFailed` workflow. Technical failure means processing
could not complete; `PaymentFailed` means the provider attempt completed with a
declined result.

## Other deferred work

- Explicit retry-payment command/API for a `FAILED` Payment, preserving prior
  PaymentTransactions.
- Persisted InventoryReservation creation and consumption.
- Authenticated real-provider webhook processing.
- Refund (`REFUNDING`/`REFUNDED`) events and append-only reversals.
- Partial payments.
- General inbox deduplication and multi-instance outbox row claiming.

## Focused verification

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Payment|FullyQualifiedName~FinancePaymentSucceeded|FullyQualifiedName~InventoryPaymentSucceeded|FullyQualifiedName~OrderPaymentSucceeded"
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~StartPaymentAtomicityTests|FullyQualifiedName~PaymentMessagingTopologyTests"
```
