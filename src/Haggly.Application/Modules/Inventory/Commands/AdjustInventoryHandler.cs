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

public sealed class AdjustInventoryHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<AdjustInventoryCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(
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
            var item = await repository.FindItemAsync(
                stall.Id,
                command.InventoryItemId,
                transactionCancellationToken)
                ?? throw new InventoryNotFoundException("The inventory item was not found.");
            EnsureExpectedVersion(item, command.ExpectedVersion);

            try
            {
                item.AdjustQuantity(
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
            return InventoryItemDto.From(item);
        }, cancellationToken);
    }

    private static void EnsureExpectedVersion(InventoryItem item, long expectedVersion)
    {
        if (item.Version != expectedVersion)
        {
            throw new InventoryConflictException(
                "The listing was changed by another request. Refresh and retry.");
        }
    }
}
