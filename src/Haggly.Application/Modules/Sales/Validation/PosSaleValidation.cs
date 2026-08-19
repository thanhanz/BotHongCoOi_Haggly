using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Modules.Sales.Validation;

public static class PosSaleValidation
{
    public static void Validate(CompletePosSaleCommand command)
    {
        if (command.StallId == Guid.Empty)
        {
            throw new PosSaleValidationException("A valid stall ID is required.");
        }

        if (command.ActorUserId == Guid.Empty)
        {
            throw new PosSaleValidationException("A valid actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ClientRequestId)
            || command.ClientRequestId.Trim().Length > 100)
        {
            throw new PosSaleValidationException(
                "A client request ID is required and must not exceed 100 characters.");
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            throw new PosSaleValidationException("At least one sale item is required.");
        }

        if (!Enum.IsDefined(command.PaymentMethod)
            || command.AmountPaid is < 0m)
        {
            throw new PosSaleValidationException("Payment method and amount are invalid.");
        }

        var listingIds = new HashSet<Guid>();
        foreach (var item in command.Items)
        {
            if (item.InventoryItemId == Guid.Empty)
            {
                throw new PosSaleValidationException("A valid daily product listing ID is required.");
            }

            if (!listingIds.Add(item.InventoryItemId))
            {
                throw new PosSaleValidationException(
                    "A daily product listing can occur only once in a POS sale.");
            }

            if (item.Quantity <= 0m)
            {
                throw new PosSaleValidationException("Sale quantity must be greater than zero.");
            }

            if (item.ExpectedInventoryVersion < 0 || item.ExpectedProductStallVersion < 0)
            {
                throw new PosSaleValidationException("Expected version cannot be negative.");
            }
        }
    }
}
