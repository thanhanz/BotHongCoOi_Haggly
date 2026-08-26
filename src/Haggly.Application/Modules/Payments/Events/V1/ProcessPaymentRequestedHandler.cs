using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Modules.Payments.Events.V1;

public sealed class ProcessPaymentRequestedHandler(
    IPaymentCommandRepository repository,
    IPaymentProvider paymentProvider,
    IPaymentAllocationRepository allocationRepository,
    IInventoryPaymentRepository inventoryRepository,
    IOutboxWriter outboxWriter,
    IPaymentUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IEventHandler<PaymentRequested>
{
    public async Task HandleAsync(
        PaymentRequested message,
        CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var payment = await repository.FindByIdAsync(
                message.PaymentId,
                transactionCancellationToken)
                ?? throw new PaymentNotFoundException(
                    $"Payment '{message.PaymentId}' was not found.");

            if (payment.Status != PaymentStatus.PENDING)
                return false;

            var occurredAt = businessClock.GetNow().ToUniversalTime();
            payment.StartProcessing(occurredAt);

            var transaction = PaymentTransaction.Create(
                Guid.NewGuid(),
                payment,
                payment.AmountDue,
                occurredAt);
            await repository.AddTransactionAsync(transaction, transactionCancellationToken);

            var providerResult = await paymentProvider.ProcessAsync(
                new PaymentProviderRequest(
                    payment.Id,
                    transaction.Id,
                    payment.AmountDue,
                    payment.Currency),
                transactionCancellationToken);

            if (providerResult.Succeeded)
            {
                transaction.MarkSucceeded(
                    providerResult.ProviderTransactionId
                        ?? throw new InvalidOperationException(
                            "A successful provider result requires a transaction ID."),
                    null,
                    null,
                    occurredAt);
                payment.MarkPaid(occurredAt);

                var sourceItems = await allocationRepository.GetTargetsForOrderAsync(
                    payment.OrderId,
                    transactionCancellationToken);
                if (sourceItems.Count == 0
                    || sourceItems.Sum(item => item.Amount) != payment.AmountDue)
                {
                    throw new InvalidOperationException(
                        "Payment allocations must exist and equal the complete payment amount.");
                }
                if (sourceItems.Select(item => item.StallFulfillmentId).Distinct().Count()
                    != sourceItems.Count
                    || sourceItems.Select(item => item.StallId).Distinct().Count()
                    != sourceItems.Count)
                {
                    throw new InvalidOperationException(
                        "Each fulfillment and stall can occur only once in a payment allocation.");
                }

                var allocations = sourceItems
                    .Select(item => PaymentAllocation.CreateSale(
                        Guid.NewGuid(),
                        transaction.Id,
                        item.StallFulfillmentId,
                        item.StallId,
                        item.Amount,
                        occurredAt))
                    .ToArray();
                foreach (var allocation in allocations)
                    await allocationRepository.AddAsync(allocation, transactionCancellationToken);

                await repository.SaveChangesAsync(transactionCancellationToken);
                await outboxWriter.WriteAsync(new PaymentSucceededEvent(
                    Guid.NewGuid(),
                    message.CorrelationId,
                    occurredAt,
                    payment.Id,
                    transaction.Id,
                    payment.OrderId,
                    payment.AmountDue,
                    payment.Currency,
                    transaction.ProviderTransactionId!,
                    allocations.Select(allocation => allocation.Id).ToArray()),
                    transactionCancellationToken);
            }
            else
            {
                var failureReason = providerResult.FailureReason
                    ?? throw new InvalidOperationException(
                        "A failed provider result requires a failure reason.");
                transaction.MarkFailed(failureReason, null, null, occurredAt);
                payment.MarkFailed(occurredAt);

                await inventoryRepository.ReleaseAsync(
                    payment.OrderId,
                    occurredAt,
                    transactionCancellationToken);

                await repository.SaveChangesAsync(transactionCancellationToken);
                await outboxWriter.WriteAsync(new PaymentFailedEvent(
                    Guid.NewGuid(),
                    message.CorrelationId,
                    occurredAt,
                    payment.Id,
                    transaction.Id,
                    payment.OrderId,
                    payment.AmountDue,
                    payment.Currency,
                    transaction.FailureReason!), transactionCancellationToken);
            }

            return true;
        }, cancellationToken);
    }
}
