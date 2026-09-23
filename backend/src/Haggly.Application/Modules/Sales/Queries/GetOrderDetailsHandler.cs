using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed class GetOrderDetailsHandler(IOrderQuery query)
    : IRequestHandler<GetOrderDetailsQuery, OrderDto>
{
    public async Task<OrderDto> Handle(
        GetOrderDetailsQuery request,
        CancellationToken cancellationToken)
    {
        OrderValidation.Validate(request);
        var order = await query.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException("The order was not found.");

        if (order.BuyerId != request.BuyerId)
        {
            throw new OrderForbiddenException("Only the buyer who owns the order can access it.");
        }

        return OrderDto.From(order);
    }
}
