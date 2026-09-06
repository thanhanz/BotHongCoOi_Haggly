using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;

namespace Haggly.Application.Modules.Inventory.Validation;

public static class InventoryValidation
{
    public static void Validate(AdjustInventoryCommand command)
    {
        if (command.StallId == Guid.Empty || command.InventoryItemId == Guid.Empty || command.ActorUserId == Guid.Empty)
            throw new InventoryValidationException("Valid stall, inventory-item, and actor IDs are required.");

        if (command.QuantityDelta == 0m || string.IsNullOrWhiteSpace(command.Reason) || command.ExpectedVersion < 0)
            throw new InventoryValidationException("A non-zero quantity, reason, and valid version are required.");
    }

    public static void Validate(GetInventoryLedgerQuery query)
    {
        if (query.StallId == Guid.Empty || query.ownerId == Guid.Empty)
            throw new InventoryValidationException("Valid stall and actor IDs are required.");

        if (query.Page <= 0 || query.PageSize is <= 0 or > 100)
            throw new InventoryValidationException("Page must be positive and page size must be between 1 and 100.");
    }
}
