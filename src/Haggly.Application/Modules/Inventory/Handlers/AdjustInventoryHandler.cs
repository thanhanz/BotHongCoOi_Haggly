using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Domain.Modules.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Handlers;

public sealed class AdjustInventoryHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<AdjustInventoryCommand, DailyProductListingDto>
{
    public async Task<DailyProductListingDto> Handle(
        AdjustInventoryCommand command,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(command);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            command.StallId,
            command.ActorUserId,
            cancellationToken);
        var occurredAt = businessClock.GetNow();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var listing = await repository.FindListingAsync(
                stall.Id,
                command.ListingId,
                transactionCancellationToken)
                ?? throw new InventoryNotFoundException("The listing was not found.");

            var session = listing.InventorySession
                ?? throw new InventoryNotFoundException("The listing session was not found.");
            EnsureSessionIsOpen(session);
            EnsureExpectedVersion(listing, command.ExpectedVersion);

            try
            {
                listing.AdjustQuantity(
                    command.QuantityDelta,
                    command.ActorUserId,
                    occurredAt,
                    command.Reason);
            }
            catch (InvalidOperationException exception)
            {
                throw new InventoryConflictException(exception.Message);
            }

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

    private static void EnsureExpectedVersion(DailyProductListing listing, long expectedVersion)
    {
        if (listing.Version != expectedVersion)
        {
            throw new InventoryConflictException(
                "The listing was changed by another request. Refresh and retry.");
        }
    }
}
