# Payments module guide

This guide records the currently executable asynchronous payment workflow.
Payment initiation, simulated provider processing, per-stall allocation,
success fan-out, Inventory payment-failure handling, and centralized
payment-event technical-fault logging are implemented.

## Responsibilities

Payments owns collection state, provider attempts, payment status, per-stall
allocation, and the payment integration events. Sales owns Order and
StallFulfillment financial state, Finance owns recognized revenue, and
Inventory owns quantity and stock-ledger mutations.

The current MVP supports one complete Payment per Order. Partial payment,
refund processing, real provider webhooks, and an explicit retry-payment API
are not implemented.

## Layer and adapter map

- Domain: `src/Haggly.Domain/Modules/Payments` owns `Payment`,
  `PaymentTransaction`, `PaymentAllocation`, `PaymentMethod`, and their status
  transitions and value constraints.
- Application: `src/Haggly.Application/Modules/Payments` owns payment start and
  provider-result orchestration. Capability-oriented ports under
  `Application/Abstractions/Payments` isolate persistence and the provider.
- Infrastructure: EF payment repositories and `EfPaymentUnitOfWork` persist the
  aggregate; `SimulatedPaymentProvider` implements `IPaymentProvider`; the
  messaging folder owns RabbitMQ, MassTransit, outbox, Inbox, consumer adapters,
  and technical-fault logging.
- API: `src/Haggly.Api/Endpoints/Payments` maps the authenticated buyer request
  to the Application command and returns `202 Accepted`. It does not process a
  provider result or mutate another module directly.

The API process currently hosts the HTTP endpoint, MassTransit bus and
consumers, and `OutboxBackgroundService`. There is no separate worker process.

## Payment processing

`POST /api/v1/payments` atomically revalidates and reserves every active
OrderItem quantity, moves the Order from `AGREED` to `PAYMENT_PENDING`, creates
a pending Payment, and appends a V1
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
5. On provider decline, mark the transaction and Payment `FAILED` and append
   `PaymentFailed` to the outbox. Inventory releases the hold asynchronously.

The Payment, PaymentTransaction, allocations, and result event are committed
in one PostgreSQL transaction. Runtime timestamps are normalized to UTC.

The configured provider is currently an in-process simulator. The provider call
occurs inside the payment-processing database transaction, which is acceptable
for this deterministic local adapter but is not the intended boundary for a
slow or callback-based real provider. Real integration must define provider
idempotency, unknown-outcome reconciliation, and transaction boundaries before
replacing the simulator.

The MassTransit adapter consumes from durable queue
`payments-payment-requested-v1`, bound to
`payments.payment-requested.v1`. Technical exceptions retry after 1, 5, and 15
seconds before following MassTransit's default
`payments-payment-requested-v1_error` transport. A definitive provider decline
is a business result and is not retried by MassTransit.

## Runtime message topology

```text
payments.payment-requested.v1
  `-> payments-payment-requested-v1
       |-> payments.payment-succeeded.v1
       |    |-> finance-payment-succeeded-v1
       |    |-> inventory-payment-succeeded-v1
       |    `-> order-payment-succeeded-v1
       `-> payments.payment-failed.v1
            |-> inventory-payment-failed-v1
            `-> order-payment-failed-v1

Fault<PaymentSucceededEvent> and Fault<PaymentFailedEvent>
  `-> payment-processing-faults-v1
       `-> structured terminal log
```

`PaymentMessagingNames` is the source of truth for these stable exchange and
queue names. MassTransit declares the current queues as durable and not
auto-delete. The event exchanges use the default MassTransit publish topology;
custom routing keys and a custom topic exchange are not implemented.

Business use cases write integration events to
`messaging.outbox_messages`; they do not publish directly to RabbitMQ. The
hosted processor publishes bounded batches and records a successful or failed
publish attempt. Concurrent multi-instance row claiming is not implemented, so
the current outbox publisher should be treated as single-active-instance.

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

The `messaging.inbox_messages` table provides atomic deduplication for the
Inventory and Order `PaymentFailedEvent` consumers. Other consumers continue to
use their existing idempotency strategies and must assume at-least-once delivery.

## Transaction and consistency boundaries

| Boundary | Atomic work |
|---|---|
| Start payment | Reserve all active OrderItem quantities, move the Order to `PAYMENT_PENDING`, create the Payment, and append `PaymentRequested`. |
| Process provider result | Update Payment and PaymentTransaction; on success create all allocations; append exactly one success or failure event. |
| Inventory failure reaction | Claim the event in Inbox and release reserved quantities. |
| Order failure reaction | Claim the event in Inbox and apply the eligible Order transition. |
| Each success reaction | Apply that module's idempotent Finance, Inventory, or Order change independently. |

Each boundary in the table is a separate PostgreSQL transaction. The workflow
is eventually consistent across modules; there is no distributed transaction
spanning Payments, Finance, Inventory, and Sales. An outbox commit guarantees
that an originating state change and its event are created together, not that
every subscriber has completed.

## Failure semantics

Haggly distinguishes a business payment result from a technical message
processing fault.

### Business payment failure

`PaymentFailed` is persisted to the outbox and published through
`payments.payment-failed.v1`. The durable `inventory-payment-failed-v1` queue
delivers it to `InventoryPaymentFailedHandler`, which atomically claims the
event in InboxMessages and releases the aggregate Inventory hold. Duplicate
delivery is a no-op. Technical failures retry after 1, 5, and 15 seconds before
the original message moves to `inventory-payment-failed-v1_error`.

The independent durable `order-payment-failed-v1` queue delivers the same event
to `OrderPaymentFailedHandler`, which atomically claims it in InboxMessages and
moves a `PAYMENT_PENDING` Order back to `AGREED`. Delayed events do not overwrite
terminal Order states. Finance remains unchanged because failed collection
recognizes no revenue.

If either payment-failure consumer continues to throw after retries at 1, 5,
and 15 seconds, MassTransit publishes `Fault<PaymentFailedEvent>` to the durable
`payment-processing-faults-v1` queue. The centralized logging consumer records
the source component, identifiers, original event ID, and exception details.
The original delivery remains available in the source endpoint's default
`_error` queue for operational replay.

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

The decision to discard source messages after publishing a centralized fault
applies only to Finance, Inventory, and Order processing of
`PaymentSucceededEvent`. Payment-failure consumers publish to the same central
fault queue but retain exhausted source messages in their `_error` queues. The
command-like `PaymentRequested` consumer still has its own retry and default
`_error` transport.

## Configuration

- `ConnectionStrings:HagglyDatabase` configures PostgreSQL.
- `RabbitMq:Host`, `Port`, `VirtualHost`, `Username`, and `Password` configure
  MassTransit and are validated at startup.
- `Outbox:Enabled`, `Interval`, and `BatchSize` configure background publishing
  and are validated at startup.
- `Payments:Simulator:Outcome` and `FailureReason` configure the local provider
  adapter.

Development values exist in `src/Haggly.Api/appsettings.Development.json` and
the local PostgreSQL/RabbitMQ services are declared in `docker-compose.yml`.
Production secrets must come from deployment configuration, not committed
settings.

## Other deferred work

- Explicit retry-payment command/API for a `FAILED` Payment, preserving prior
  PaymentTransactions.
- Reservation expiration and abandoned-payment release.
- Authenticated real-provider webhook processing.
- Refund (`REFUNDING`/`REFUNDED`) events and append-only reversals.
- Partial payments.
- Inbox adoption by consumers other than Inventory and Order payment failure,
  and multi-instance outbox row claiming.
- Durable technical-fault storage and incident lifecycle management.
- Replay or reconciliation for failed `PaymentSucceededEvent` consumers.
- Loki log collection and Grafana dashboards or alerts.

## Focused verification

The unit suite contains Domain/Application payment behavior, result handlers,
Inbox failure handlers, and structured fault-mapping/consumer coverage.
`StartPaymentAtomicityTests` exercises PostgreSQL transaction and idempotency
boundaries. `PaymentMessagingTopologyTests` requires reachable RabbitMQ and its
management API; it verifies durable topology and centralized fault delivery.

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Payment|FullyQualifiedName~FinancePaymentSucceeded|FullyQualifiedName~InventoryPaymentSucceeded|FullyQualifiedName~OrderPaymentSucceeded|FullyQualifiedName~Infrastructure.Messaging"
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~StartPaymentAtomicityTests|FullyQualifiedName~PaymentMessagingTopologyTests"
```
