using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Validation;
using Haggly.Domain.Modules.Sales;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed class CreateOrderHandler(
    IOrderCommandRepository repository,
    IOrderCatalog catalog,
    IBusinessClock businessClock)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        OrderValidation.Validate(command);

        var snapshots = await catalog.GetOrderLinesAsync(
            command.Items.Select(item => item.InventoryItemId).ToArray(),
            cancellationToken);
        if (snapshots.GroupBy(snapshot => snapshot.InventoryItemId).Any(group => group.Count() > 1))
        {
            throw new OrderValidationException(
                "The order catalog returned duplicate inventory items.");
        }

        var byId = snapshots.ToDictionary(snapshot => snapshot.InventoryItemId);

        if (byId.Count != command.Items.Count)
        {
            throw new OrderValidationException(
                "One or more inventory items are not available for ordering.");
        }

        var inputs = new List<OrderItemInput>(command.Items.Count);
        foreach (var item in command.Items)
        {
            var snapshot = byId[item.InventoryItemId];
            if (item.Quantity > snapshot.AvailableQuantity)
            {
                throw new OrderValidationException(
                    $"The requested quantity for '{snapshot.ProductName}' exceeds available inventory.");
            }

            inputs.Add(new OrderItemInput(
                snapshot.InventoryItemId,
                snapshot.StallId,
                snapshot.ProductName,
                snapshot.SellingUnit,
                snapshot.UnitPrice,
                item.Quantity,
                item.Notes));
        }

        var order = Order.Place(
            Guid.NewGuid(),
            command.BuyerId,
            inputs,
            businessClock.GetNow());
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
