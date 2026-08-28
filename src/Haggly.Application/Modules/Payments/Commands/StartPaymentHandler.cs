using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Dtos;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using MediatR;

namespace Haggly.Application.Modules.Payments.Commands;

public sealed class StartPaymentHandler(
    IPaymentCommandRepository repository,
    IOrderCommandRepository orderRepository,
    IInventoryPaymentRepository inventoryRepository,
    IOutboxWriter outboxWriter,
    IPaymentUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<StartPaymentCommand, PaymentDto>
{
    public Task<PaymentDto> Handle(
        StartPaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.OrderId == Guid.Empty)
            throw new PaymentValidationException("A valid order ID is required.");
        if (command.BuyerId == Guid.Empty)
            throw new PaymentValidationException("A valid buyer ID is required.");

        return unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var order = await orderRepository.FindForPaymentAsync(
                command.OrderId,
                transactionCancellationToken)
                ?? throw new PaymentNotFoundException("The order was not found.");

            if (order.BuyerId != command.BuyerId)
                throw new PaymentForbiddenException("The order belongs to another buyer.");
            if (order.Status is not OrderStatus.AGREED and not OrderStatus.PAYMENT_PENDING)
                throw new PaymentConflictException("The order is not ready for payment.");
            if (order.TotalToCharge <= 0)
                throw new PaymentConflictException("The order does not have a positive payable amount.");
            if (await repository.FindByOrderIdAsync(order.Id, transactionCancellationToken) is not null)
                throw new PaymentConflictException("A payment already exists for this order.");

            var occurredAt = businessClock.GetNow().ToUniversalTime();
            await inventoryRepository.ReserveAsync(
                order.Id,
                occurredAt,
                transactionCancellationToken);

            order.StartPayment(occurredAt);
            await orderRepository.SaveChangesAsync(transactionCancellationToken);

            var payment = Payment.Create(
                Guid.NewGuid(),
                order.Id,
                order.TotalToCharge,
                order.Currency,
                occurredAt);

            await repository.AddAsync(payment, transactionCancellationToken);
            await repository.SaveChangesAsync(transactionCancellationToken);

            await outboxWriter.WriteAsync(new PaymentRequested(
                Guid.NewGuid(),
                payment.Id,
                occurredAt,
                payment.Id,
                payment.OrderId,
                payment.AmountDue,
                payment.Currency), transactionCancellationToken);

            return PaymentDto.From(payment);
        }, cancellationToken);
    }
}
