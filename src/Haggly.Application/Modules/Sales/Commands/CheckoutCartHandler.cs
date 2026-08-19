using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Handlers;
using Haggly.Application.Modules.Sales.Validation;
using Haggly.Domain.Modules.Sales;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class CheckoutCartHandler(
    ICartCommandRepository cartRepository,
    ICartCatalog catalog,
    IOrderCommandRepository orderRepository,
    ICartCheckoutUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<CheckoutCartCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CheckoutCartCommand command,
        CancellationToken cancellationToken)
    {
        CartValidation.Validate(command);
        var cart = await cartRepository.FindByBuyerIdAsync(command.BuyerId, cancellationToken)
            ?? throw new CartNotFoundException("The buyer cart was not found.");

        if (cart.Items.Count == 0)
        {
            throw new CartValidationException("The cart must contain at least one item.");
        }

        var snapshots = await catalog.GetItemsAsync(
            cart.Items.Select(item => item.InventoryItemId).ToArray(),
            cancellationToken);
        if (snapshots.Select(item => item.InventoryItemId).Distinct().Count() != cart.Items.Count)
        {
            throw new CartValidationException("One or more cart items are not available for checkout.");
        }

        var inputs = new List<OrderItemInput>(cart.Items.Count);
        foreach (var item in cart.Items)
        {
            var snapshot = CartHandlerHelpers.RequireSnapshot(snapshots, item.InventoryItemId);
            CartHandlerHelpers.EnsureQuantity(snapshot, item.Quantity);
            inputs.Add(new OrderItemInput(
                snapshot.InventoryItemId,
                snapshot.StallId,
                snapshot.ProductName,
                snapshot.SellingUnit,
                snapshot.UnitPrice,
                item.Quantity,
                item.Notes));
        }

        var order = Order.Place(
            Guid.NewGuid(),
            command.BuyerId,
            inputs,
            businessClock.GetNow());

        await unitOfWork.ExecuteAsync(async ct =>
        {
            await orderRepository.AddAsync(order, ct);
            cart.Clear(businessClock.GetNow());
            await cartRepository.SaveChangesAsync(ct);
            return order;
        }, cancellationToken);

        return OrderDto.From(order);
    }
}
