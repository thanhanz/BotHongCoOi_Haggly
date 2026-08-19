using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;

namespace Haggly.Application.Modules.Sales.Validation;

public static class CartValidation
{
    public static void Validate(GetCartQuery query)
    {
        ValidateBuyer(query.BuyerId);
    }

    public static void Validate(AddCartItemCommand command)
    {
        ValidateBuyer(command.BuyerId);
        ValidateItem(command.InventoryItemId, command.Quantity, command.Notes);
    }

    public static void Validate(UpdateCartItemCommand command)
    {
        ValidateBuyer(command.BuyerId);
        if (command.CartItemId == Guid.Empty)
        {
            throw new CartValidationException("A valid cart item ID is required.");
        }

        ValidateQuantityAndNotes(command.Quantity, command.Notes);
    }

    public static void Validate(RemoveCartItemCommand command)
    {
        ValidateBuyer(command.BuyerId);
        if (command.CartItemId == Guid.Empty)
        {
            throw new CartValidationException("A valid cart item ID is required.");
        }
    }

    public static void Validate(ClearCartCommand command)
        => ValidateBuyer(command.BuyerId);

    public static void Validate(CheckoutCartCommand command)
        => ValidateBuyer(command.BuyerId);

    public static void ValidateBuyer(Guid buyerId)
    {
        if (buyerId == Guid.Empty)
        {
            throw new CartValidationException("A valid buyer ID is required.");
        }
    }

    private static void ValidateItem(Guid inventoryItemId, decimal quantity, string? notes)
    {
        if (inventoryItemId == Guid.Empty)
        {
            throw new CartValidationException("A valid inventory item ID is required.");
        }

        ValidateQuantityAndNotes(quantity, notes);
    }

    private static void ValidateQuantityAndNotes(decimal quantity, string? notes)
    {
        if (quantity <= 0m)
        {
            throw new CartValidationException("Cart quantity must be greater than zero.");
        }

        if (notes?.Length > 500)
        {
            throw new CartValidationException("Cart item notes must not exceed 500 characters.");
        }
    }
}
