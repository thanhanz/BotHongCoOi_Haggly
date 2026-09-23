using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;

namespace Haggly.Application.Modules.Sales.Validation;

public static class OrderValidation
{
    public static void Validate(CreateOrderCommand command)
    {
        if (command.BuyerId == Guid.Empty)
        {
            throw new OrderValidationException("A valid buyer ID is required.");
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            throw new OrderValidationException("At least one order item is required.");
        }

        var itemIds = new HashSet<Guid>();
        foreach (var item in command.Items)
        {
            if (item is null || item.InventoryItemId == Guid.Empty)
            {
                throw new OrderValidationException("A valid inventory item ID is required.");
            }

            if (!itemIds.Add(item.InventoryItemId))
            {
                throw new OrderValidationException(
                    "An inventory item can occur only once in an order.");
            }

            if (item.Quantity <= 0m)
            {
                throw new OrderValidationException("Order quantity must be greater than zero.");
            }

            if (item.Notes?.Length > 500)
            {
                throw new OrderValidationException("Order item notes must not exceed 500 characters.");
            }
        }
    }

    public static void Validate(CancelOrderCommand command)
    {
        if (command.OrderId == Guid.Empty || command.BuyerId == Guid.Empty)
        {
            throw new OrderValidationException("Valid order and buyer IDs are required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
        {
            throw new OrderValidationException(
                "A cancellation reason is required and must not exceed 500 characters.");
        }
    }

    public static void Validate(GetOrdersQuery query)
    {
        if (query.BuyerId == Guid.Empty)
        {
            throw new OrderValidationException("A valid buyer ID is required.");
        }

        ValidatePage(query.Page, query.PageSize);
    }

    public static void Validate(GetOrderDetailsQuery query)
    {
        if (query.OrderId == Guid.Empty || query.BuyerId == Guid.Empty)
        {
            throw new OrderValidationException("Valid order and buyer IDs are required.");
        }
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new OrderValidationException(
                "Page must be at least 1 and page size must be between 1 and 100.");
        }
    }
}
