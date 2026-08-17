using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class CancelOrderHandler(
    IOrderCommandRepository repository,
    IBusinessClock businessClock)
    : IRequestHandler<CancelOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        OrderValidation.Validate(command);
        var order = await repository.FindByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException("The order was not found.");

        if (order.BuyerId != command.BuyerId)
        {
            throw new OrderForbiddenException("Only the buyer who owns the order can cancel it.");
        }

        try
        {
            order.Cancel(command.Reason, businessClock.GetNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new OrderConflictException(exception.Message);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
