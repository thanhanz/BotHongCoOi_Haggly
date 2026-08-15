using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Domain.Modules.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Handlers;

public sealed class CloseInventorySessionHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<CloseInventorySessionCommand, InventorySessionDto>
{
    public async Task<InventorySessionDto> Handle(
        CloseInventorySessionCommand command,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(command);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            command.StallId,
            command.ActorUserId,
            cancellationToken);
        var businessDate = businessClock.GetBusinessDate();
        var closedAt = businessClock.GetNow();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var session = await repository.FindSessionAsync(
                stall.Id,
                businessDate,
                transactionCancellationToken)
                ?? throw new InventoryNotFoundException(
                    "The current inventory session was not found.");

            try
            {
                session.Close(command.ActorUserId, closedAt);
            }
            catch (InvalidOperationException exception)
            {
                throw new InventoryConflictException(exception.Message);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InventoryConflictException(exception.Message);
            }

            await repository.SaveChangesAsync(transactionCancellationToken);
            return InventorySessionDto.From(session);
        }, cancellationToken);
    }
}
