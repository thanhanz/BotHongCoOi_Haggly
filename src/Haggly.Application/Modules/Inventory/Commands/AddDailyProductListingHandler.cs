using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Domain.Modules.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed class AddDailyProductListingHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<AddDailyProductListingCommand, DailyProductListingDto>
{
    public async Task<DailyProductListingDto> Handle(
        AddDailyProductListingCommand command,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(command);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            command.StallId,
            command.ActorUserId,
            cancellationToken);
        var businessDate = businessClock.GetBusinessDate();
        var occurredAt = businessClock.GetNow();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var session = await repository.FindSessionAsync(
                stall.Id,
                businessDate,
                transactionCancellationToken)
                ?? throw new InventoryNotFoundException(
                    "The current inventory session was not found.");

            EnsureSessionIsOpen(session);

            var productStall = await InventoryStallAccess.RequireActiveProductStallAsync(
                references,
                stall.Id,
                command.Listing.ProductStallId,
                transactionCancellationToken);

            if (await repository.ListingExistsAsync(
                    session.Id,
                    productStall.Id,
                    transactionCancellationToken))
            {
                throw new InventoryConflictException(
                    "A listing for this product already exists in the session.");
            }

            var productName = productStall.DisplayName ?? productStall.Product!.Name;
            var publicUnitPrice = command.Listing.PublicUnitPrice ?? productStall.DefaultUnitPrice;
            var listing = DailyProductListing.Open(
                session.Id,
                productStall.Id,
                productName,
                productStall.SellingUnit,
                publicUnitPrice,
                command.Listing.OpeningQuantity,
                command.ActorUserId,
                occurredAt);

            session.DailyProductListings.Add(listing);
            await repository.AddListingAsync(listing, transactionCancellationToken);
            await repository.SaveChangesAsync(transactionCancellationToken);
            return DailyProductListingDto.From(listing);
        }, cancellationToken);
    }

    private static void EnsureSessionIsOpen(InventorySession session)
    {
        try
        {
            session.EnsureOpen();
        }
        catch (InvalidOperationException exception)
        {
            throw new InventoryConflictException(exception.Message);
        }
    }
}
