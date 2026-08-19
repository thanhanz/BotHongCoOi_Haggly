using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Handlers;
using Haggly.Application.Modules.Sales.Validation;
using Haggly.Domain.Modules.Sales;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class AddCartItemHandler(
    ICartCommandRepository repository,
    ICartCatalog catalog,
    ICartQuery query,
    IBusinessClock businessClock)
    : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(
        AddCartItemCommand command,
        CancellationToken cancellationToken)
    {
        CartValidation.Validate(command);
        var snapshots = await catalog.GetItemsAsync([command.InventoryItemId], cancellationToken);
        var snapshot = CartHandlerHelpers.RequireSnapshot(snapshots, command.InventoryItemId);
        CartHandlerHelpers.EnsureQuantity(snapshot, command.Quantity);

        var existingCart = await repository.FindByBuyerIdAsync(command.BuyerId, cancellationToken);
        var cart = existingCart ?? Cart.Create(command.BuyerId, businessClock.GetNow());

        try
        {
            cart.AddItem(command.InventoryItemId, command.Quantity, command.Notes, businessClock.GetNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new CartConflictException(exception.Message);
        }

        if (existingCart is null)
        {
            await repository.AddAsync(cart, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return await CartHandlerHelpers.ReadAsync(query, command.BuyerId, cancellationToken);
    }
}
