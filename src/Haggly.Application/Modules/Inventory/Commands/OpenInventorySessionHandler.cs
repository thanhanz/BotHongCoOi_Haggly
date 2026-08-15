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

public sealed class OpenInventorySessionHandler(
    IInventoryCommandRepository repository,
    IInventoryReferenceQuery references,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock)
    : IRequestHandler<OpenInventorySessionCommand, InventorySessionDto>
{
    public async Task<InventorySessionDto> Handle(
        OpenInventorySessionCommand command,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(command);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            command.StallId,
            command.ActorUserId,
            cancellationToken);
        var businessDate = businessClock.GetBusinessDate();
        var openedAt = businessClock.GetNow();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            if (await repository.FindSessionAsync(
                    stall.Id,
                    businessDate,
                    transactionCancellationToken) is not null)
            {
                throw new InventoryConflictException(
                    "An inventory session already exists for this stall and business date.");
            }

            var session = InventorySession.Open(
                stall.Id,
                businessDate,
                openedAt,
                command.ActorUserId,
                command.Notes);

            await repository.AddSessionAsync(session, transactionCancellationToken);

            foreach (var input in command.Listings)
            {
                var productStall = await InventoryStallAccess.RequireActiveProductStallAsync(
                    references,
                    stall.Id,
                    input.ProductStallId,
                    transactionCancellationToken);
                var productName = productStall.DisplayName ?? productStall.Product!.Name;
                var price = input.PublicUnitPrice ?? productStall.DefaultUnitPrice;
                var listing = DailyProductListing.Open(
                    session.Id,
                    productStall.Id,
                    productName,
                    productStall.SellingUnit,
                    price,
                    input.OpeningQuantity,
                    command.ActorUserId,
                    openedAt);

                session.DailyProductListings.Add(listing);
                await repository.AddListingAsync(listing, transactionCancellationToken);
            }

            await repository.SaveChangesAsync(transactionCancellationToken);
            return InventorySessionDto.From(session);
        }, cancellationToken);
    }
}
