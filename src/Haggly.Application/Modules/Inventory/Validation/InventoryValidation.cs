using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Validation;

public static class InventoryValidation
{
    private const int ReasonMaximumLength = 500;

    public static void Validate(OpenInventorySessionCommand command)
    {
        ValidateActorAndStall(command.StallId, command.ActorUserId);
        ArgumentNullException.ThrowIfNull(command.Listings);

        var productStallIds = new HashSet<Guid>();
        foreach (var listing in command.Listings)
        {
            Validate(listing);
            if (!productStallIds.Add(listing.ProductStallId))
            {
                throw new InventoryValidationException(
                    "A product-stall can occur only once in an inventory session.");
            }
        }
    }

    public static void Validate(AddDailyProductListingCommand command)
    {
        ValidateActorAndStall(command.StallId, command.ActorUserId);
        ArgumentNullException.ThrowIfNull(command.Listing);
        Validate(command.Listing);
    }

    public static void Validate(InventoryListingInput listing)
    {
        if (listing.ProductStallId == Guid.Empty)
        {
            throw new InventoryValidationException("A valid product-stall ID is required.");
        }

        if (listing.OpeningQuantity < 0m)
        {
            throw new InventoryValidationException("Opening quantity cannot be negative.");
        }

        if (listing.PublicUnitPrice < 0m)
        {
            throw new InventoryValidationException("Public unit price cannot be negative.");
        }
    }

    public static void Validate(UpdateDailyProductListingCommand command)
    {
        ValidateActorAndStall(command.StallId, command.ActorUserId);
        ValidateListingId(command.ListingId);
        ValidateExpectedVersion(command.ExpectedVersion);

        if (command.PublicUnitPrice < 0m)
        {
            throw new InventoryValidationException("Public unit price cannot be negative.");
        }

        if (command.Status is not null
            && (!Enum.IsDefined(command.Status.Value)
                || command.Status is DailyListingStatus.SOLD_OUT))
        {
            throw new InventoryValidationException(
                "Listing status must be AVAILABLE or HIDDEN; SOLD_OUT is derived.");
        }
    }

    public static void Validate(AdjustInventoryCommand command)
    {
        ValidateActorAndStall(command.StallId, command.ActorUserId);
        ValidateListingId(command.ListingId);
        ValidateExpectedVersion(command.ExpectedVersion);

        if (command.QuantityDelta == 0m)
        {
            throw new InventoryValidationException("Quantity delta must not be zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > ReasonMaximumLength)
        {
            throw new InventoryValidationException(
                $"A reason is required and must not exceed {ReasonMaximumLength} characters.");
        }
    }

    public static void Validate(CloseInventorySessionCommand command)
        => ValidateActorAndStall(command.StallId, command.ActorUserId);

    public static void Validate(GetCurrentInventorySessionQuery query)
        => ValidateStall(query.StallId);

    public static void Validate(GetPreviousInventorySessionQuery query)
        => ValidateStall(query.StallId);

    public static void Validate(GetInventoryLedgerQuery query)
    {
        ValidateStall(query.StallId);
        if (query.ListingId == Guid.Empty)
        {
            throw new InventoryValidationException("Listing ID must be valid when provided.");
        }

        if (query.TransactionType is not null && !Enum.IsDefined(query.TransactionType.Value))
        {
            throw new InventoryValidationException("Transaction type must be valid when provided.");
        }

        ValidatePaging(query.Page, query.PageSize);
    }

    private static void ValidateActorAndStall(Guid stallId, Guid actorUserId)
    {
        ValidateStall(stallId);
        if (actorUserId == Guid.Empty)
        {
            throw new InventoryValidationException("A valid actor ID is required.");
        }
    }

    private static void ValidateStall(Guid stallId)
    {
        if (stallId == Guid.Empty)
        {
            throw new InventoryValidationException("A valid stall ID is required.");
        }
    }

    private static void ValidateListingId(Guid listingId)
    {
        if (listingId == Guid.Empty)
        {
            throw new InventoryValidationException("A valid listing ID is required.");
        }
    }

    private static void ValidateExpectedVersion(long expectedVersion)
    {
        if (expectedVersion < 0)
        {
            throw new InventoryValidationException("Expected version cannot be negative.");
        }
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new InventoryValidationException("Valid page and page size are required.");
        }
    }
}
