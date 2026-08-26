# Payments module guide

This guide records the currently executable asynchronous payment workflow.
Payment initiation, simulated provider processing, per-stall allocation,
success fan-out, and centralized technical-fault logging are implemented.
Business reactions to `PaymentFailedEvent` remain deferred.

## Responsibilities

Payments owns collection state, provider attempts, payment status, per-stall
allocation, and the payment integration events. Sales owns Order and
StallFulfillment financial state, Finance owns recognized revenue, and
Inventory owns quantity and stock-ledger mutations.

The current MVP supports one complete Payment per Order. Partial payment,
refund processing, real provider webhooks, and an explicit retry-payment API
are not implemented.

## Payment processing

`POST /api/v1/payments` atomically revalidates and reserves every active
OrderItem quantity, creates a pending Payment, and appends a V1
`PaymentRequested` event to the PostgreSQL outbox. The hosted outbox processor
publishes that event to RabbitMQ. A separate reservation entity is not stored;
InventoryItem keeps the aggregate reserved quantity.

`ProcessPaymentRequestedHandler` performs the following workflow:

1. Load a `PENDING` Payment and move it to `PROCESSING`.
2. Create a pending PaymentTransaction.
3. Invoke `IPaymentProvider`.
4. On success, mark the transaction `SUCCEEDED`, mark the Payment `PAID`, create
   one PaymentAllocation per active StallFulfillment, and append
   `PaymentSucceeded` to the outbox.
5. On provider decline, release the OrderItem quantities, mark the transaction
   and Payment `FAILED`, and append `PaymentFailed` to the outbox.

The Payment, PaymentTransaction, allocations, and result event are committed
in one PostgreSQL transaction. Runtime timestamps are normalized to UTC.

The MassTransit adapter consumes from durable queue
`payments-payment-requested-v1`, bound to
`payments.payment-requested.v1`. Technical exceptions retry after 1, 5, and 15
seconds before following MassTransit's default
`payments-payment-requested-v1_error` transport. A definitive provider decline
is a business result and is not retried by MassTransit.

## PaymentSucceeded fan-out

The outbox publishes `PaymentSucceeded` once to
`payments.payment-succeeded.v1`. RabbitMQ copies it to three independent durable
queues, so the modules process the event independently and can succeed or fail
without acknowledging another module's delivery:

| Module | Queue | Application handler | Persisted effect |
|---|---|---|---|
| Finance | `finance-payment-succeeded-v1` | `FinancePaymentSucceededHandler` | Append one RevenueLedger sale per PaymentAllocation. |
| Inventory | `inventory-payment-succeeded-v1` | `InventoryPaymentSucceededHandler` | Decrease current and reserved quantities and append `ONLINE_SALE` InventoryLedger rows. |
| Sales/Order | `order-payment-succeeded-v1` | `OrderPaymentSucceededHandler` | Set `Order.TotalPaid`, move the Order to `PAID`, and apply each allocation to `StallFulfillment.PaidAmount`. |

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

## Failure semantics

Haggly distinguishes a business payment result from a technical message
processing fault.

### Business payment failure

`PaymentFailed` is currently persisted to the outbox and published through the
registered `payments.payment-failed.v1` exchange, but there are no downstream
module queues or handlers for it yet. It represents a provider attempt that
completed with a definitive unsuccessful result. It does not represent an
exception raised by Finance, Inventory, or Order while processing
`PaymentSucceededEvent`.

A definitive provider decline releases the aggregate Inventory hold in the same
database transaction that marks the Payment and attempt failed and writes the
failure event. Finance remains unchanged because failed collection recognizes
no revenue. Sales/Order does not yet react to `PaymentFailed`.

### PaymentSucceeded consumer fault

Finance, Inventory, and Order do not retry a failed
`PaymentSucceededEvent`. When any of their Application handlers throws:

1. The exception escapes its MassTransit consumer adapter.
2. MassTransit publishes `Fault<PaymentSucceededEvent>`, containing the
   original event, fault identifiers, exception information, timestamp, and
   host information.
3. The source endpoint uses `DiscardFaultedMessages()`, so the original event
   is not moved to a module-specific `_error` queue.
4. The durable `payment-processing-faults-v1` queue receives every published
   `Fault<PaymentSucceededEvent>`.
5. `LoggingFaultConsumer<PaymentSucceededEvent>` maps the source queue to the
   `Finance`, `Inventory`, or `Order` component and writes one structured
   `ILogger` error record.
6. The centralized fault message is acknowledged after logging succeeds.

The structured record includes component, event type, fault ID, faulted
message ID, correlation ID, original event ID, source address, host machine,
exception types, messages, and stack traces. ASP.NET Core's configured logging
providers currently write the record to the terminal. Loki storage and Grafana
querying, dashboards, and alerts are not implemented.

This is deliberately a logging-only failure path. The source message is not
retained for broker replay, no compensation or reconciliation is started, and
the Payment can remain `PAID` while one downstream module is inconsistent.
Operational recovery, durable incident storage, replay, reconciliation, and
downstream retry policies remain future work.

The single centralized fault queue decision applies only to Finance,
Inventory, and Order processing of `PaymentSucceededEvent`. The command-like
`PaymentRequested` consumer still has its own retry and default `_error`
transport.

## Other deferred work

- Explicit retry-payment command/API for a `FAILED` Payment, preserving prior
  PaymentTransactions.
- Reservation expiration and abandoned-payment release.
- Authenticated real-provider webhook processing.
- Refund (`REFUNDING`/`REFUNDED`) events and append-only reversals.
- Partial payments.
- General inbox deduplication and multi-instance outbox row claiming.
- Durable technical-fault storage and incident lifecycle management.
- Replay or reconciliation for failed `PaymentSucceededEvent` consumers.
- Loki log collection and Grafana dashboards or alerts.

## Focused verification

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Payment|FullyQualifiedName~FinancePaymentSucceeded|FullyQualifiedName~InventoryPaymentSucceeded|FullyQualifiedName~OrderPaymentSucceeded|FullyQualifiedName~Infrastructure.Messaging"
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~StartPaymentAtomicityTests|FullyQualifiedName~PaymentMessagingTopologyTests"
```
