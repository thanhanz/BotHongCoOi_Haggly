using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Validation;
using Haggly.Domain.Modules.Finance;
using Haggly.Domain.Modules.Sales;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class CompletePosSaleHandler(
    IPosSaleCommandRepository repository,
    IInventorySaleRecorder inventory,
    IPosSaleUnitOfWork unitOfWork,
    IBusinessClock businessClock,
    IRevenueLedgerRepository? revenueLedger = null)
    : IRequestHandler<CompletePosSaleCommand, PosSaleDto>
{
    public async Task<PosSaleDto> Handle(
        CompletePosSaleCommand command,
        CancellationToken cancellationToken)
    {
        PosSaleValidation.Validate(command);

        var existing = await repository.FindByClientRequestIdAsync(
            command.StallId,
            command.ClientRequestId.Trim(),
            cancellationToken);

        if (existing is not null)
        {
            return PosSaleDto.From(existing);
        }

        var saleId = Guid.NewGuid();
        var occurredAt = businessClock.GetNow();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var snapshots = await inventory.RecordPosSaleAsync(
                command.StallId,
                saleId,
                command.ActorUserId,
                command.Items
                    .Select(item => new InventorySaleLine(
                        item.InventoryItemId,
                        item.Quantity,
                        item.ExpectedInventoryVersion,
                        item.ExpectedProductStallVersion))
                    .ToArray(),
                occurredAt,
                transactionCancellationToken);

            var sale = PosSale.Complete(
                saleId,
                command.StallId,
                command.ActorUserId,
                command.ClientRequestId.Trim(),
                snapshots
                .Select(item => new PosSaleItemInput(
                        item.InventoryItemId,
                        item.ProductNameSnapshot,
                        item.SellingUnitSnapshot,
                    item.UnitPrice,
                    item.Quantity))
                    .ToArray(),
                occurredAt,
                command.PaymentMethod,
                command.AmountPaid);

            if (revenueLedger is not null)
            {
                await revenueLedger.AddAsync(
                    RevenueLedger.CreatePosSaleEntry(
                        sale.Id,
                        sale.StallId,
                        sale.TotalAmount,
                        sale.CompletedAt),
                    transactionCancellationToken);
            }
            await repository.AddAsync(sale, transactionCancellationToken);
            await repository.SaveChangesAsync(transactionCancellationToken);
            return PosSaleDto.From(sale);
        }, cancellationToken);
    }
}
