using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed class AddInventoryItemHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<AddInventoryItemCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(AddInventoryItemCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references, command.StallId, command.ActorUserId, cancellationToken);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var inventory = await repository.FindInventoryAsync(stall.Id, transactionCancellationToken)
                ?? throw new InventoryNotFoundException("The stall inventory was not found.");
            var productStall = await InventoryStallAccess.RequireActiveProductStallAsync(
                references, stall.Id, command.ProductStallId, transactionCancellationToken);

            if (await repository.ItemExistsAsync(inventory.Id, productStall.Id, transactionCancellationToken))
            {
                throw new InventoryConflictException("The product already exists in this inventory.");
            }

            var item = inventory.AddItem(
                productStall.Id, command.CurrentQuantity, command.ActorUserId, businessClock.GetNow());
            await repository.AddItemAsync(item, transactionCancellationToken);
            await repository.SaveChangesAsync(transactionCancellationToken);
            return InventoryItemDto.From(item);
        }, cancellationToken);
    }

    private static void Validate(AddInventoryItemCommand command)
    {
        if (command.StallId == Guid.Empty || command.ActorUserId == Guid.Empty || command.ProductStallId == Guid.Empty)
        {
            throw new InventoryValidationException("Valid stall, actor, and stall-product IDs are required.");
        }

        if (command.CurrentQuantity < 0m)
        {
            throw new InventoryValidationException("Current quantity cannot be negative.");
        }
    }
}
