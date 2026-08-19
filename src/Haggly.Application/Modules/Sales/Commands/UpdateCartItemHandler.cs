using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Handlers;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class UpdateCartItemHandler(
    ICartCommandRepository repository,
    ICartCatalog catalog,
    ICartQuery query,
    IBusinessClock businessClock)
    : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(
        UpdateCartItemCommand command,
        CancellationToken cancellationToken)
    {
        CartValidation.Validate(command);
        var cart = await repository.FindByBuyerIdAsync(command.BuyerId, cancellationToken)
            ?? throw new CartNotFoundException("The buyer cart was not found.");
        var item = cart.Items.SingleOrDefault(value => value.Id == command.CartItemId)
            ?? throw new CartNotFoundException("The cart item was not found.");

        var snapshots = await catalog.GetItemsAsync([item.InventoryItemId], cancellationToken);
        var snapshot = CartHandlerHelpers.RequireSnapshot(snapshots, item.InventoryItemId);
        CartHandlerHelpers.EnsureQuantity(snapshot, command.Quantity);
        cart.UpdateItem(command.CartItemId, command.Quantity, command.Notes, businessClock.GetNow());

        await repository.SaveChangesAsync(cancellationToken);
        return await CartHandlerHelpers.ReadAsync(query, command.BuyerId, cancellationToken);
    }
}
