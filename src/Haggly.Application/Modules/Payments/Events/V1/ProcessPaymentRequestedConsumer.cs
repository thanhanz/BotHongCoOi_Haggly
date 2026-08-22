using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Modules.Payments.Events.V1;

public sealed class ProcessPaymentRequestedConsumer(
    IPaymentCommandRepository repository,
    IPaymentProvider paymentProvider,
    IOutboxWriter outboxWriter,
    IPaymentUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IDomainEventConsumer<PaymentRequested>
{
    public async Task ConsumeAsync(
        PaymentRequested integrationEvent,
        CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var payment = await repository.FindByIdAsync(
                integrationEvent.PaymentId,
                transactionCancellationToken)
                ?? throw new PaymentNotFoundException(
                    $"Payment '{integrationEvent.PaymentId}' was not found.");

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

                await repository.SaveChangesAsync(transactionCancellationToken);
                await outboxWriter.WriteAsync(new PaymentSucceeded(
                    Guid.NewGuid(),
                    integrationEvent.CorrelationId,
                    occurredAt,
                    payment.Id,
                    transaction.Id,
                    payment.OrderId,
                    payment.AmountDue,
                    payment.Currency,
                    transaction.ProviderTransactionId!), transactionCancellationToken);
            }
            else
            {
                var failureReason = providerResult.FailureReason
                    ?? throw new InvalidOperationException(
                        "A failed provider result requires a failure reason.");
                transaction.MarkFailed(failureReason, null, null, occurredAt);
                payment.MarkFailed(occurredAt);

                await repository.SaveChangesAsync(transactionCancellationToken);
                await outboxWriter.WriteAsync(new PaymentFailed(
                    Guid.NewGuid(),
                    integrationEvent.CorrelationId,
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
