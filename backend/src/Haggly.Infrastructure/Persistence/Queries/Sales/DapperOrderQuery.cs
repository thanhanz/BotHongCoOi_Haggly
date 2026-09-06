using Dapper;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperOrderQuery(DapperDbContext db) : IOrderQuery
{
    public async Task<PagedResult<Order>> GetPageAsync(
        Guid buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM sales.orders
            WHERE "BuyerId" = @BuyerId;

            SELECT "Id", "OrderNo", "BuyerId", "Status", "TotalToCharge", "TotalPaid",
                   "Currency", "PlacedAt", "PaymentDueAt", "CancelledAt", "CancellationReason",
                   "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.orders
            WHERE "BuyerId" = @BuyerId
            ORDER BY "PlacedAt" DESC NULLS LAST, "Id" DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new
            {
                BuyerId = buyerId,
                Offset = checked((page - 1) * pageSize),
                PageSize = pageSize
            },
            cancellationToken: cancellationToken));

        var total = checked((int)await results.ReadSingleAsync<long>());
        var orders = (await results.ReadAsync<Order>()).AsList();
        return new PagedResult<Order>(orders, page, pageSize, total);
    }

    public async Task<Order?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "OrderNo", "BuyerId", "Status", "TotalToCharge", "TotalPaid",
                   "Currency", "PlacedAt", "PaymentDueAt", "CancelledAt", "CancellationReason",
                   "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.orders
            WHERE "Id" = @OrderId;

            SELECT "Id", "OrderId", "StallId", "FulfillmentNo", "Status", "Subtotal", "FinalAmount",
                   "PaidAmount", "PickupCode", "PreparedAt", "ReadyAt", "PickedUpAt",
                   "PickupConfirmedBy", "CancelledAt", "CancellationReason",
                   "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.stall_fulfillments
            WHERE "OrderId" = @OrderId
            ORDER BY "Id";

            SELECT item."Id", item."StallFulfillmentId", item."InventoryItemId",
                   item."ProductNameSnapshot", item."SellingUnitSnapshot",
                   item."PublicUnitPriceSnapshot", item."FinalUnitPrice", item."FinalQuantity",
                   item."LineTotal", item."IsNegotiated", item."Status", item."Notes",
                   item."CreatedAt", item."CreatedBy", item."UpdatedAt", item."UpdatedBy"
            FROM sales.order_items item
            INNER JOIN sales.stall_fulfillments fulfillment
                ON fulfillment."Id" = item."StallFulfillmentId"
            WHERE fulfillment."OrderId" = @OrderId
            ORDER BY item."Id";
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new { OrderId = orderId },
            cancellationToken: cancellationToken));

        var order = await results.ReadSingleOrDefaultAsync<Order>();
        if (order is null)
        {
            return null;
        }

        var fulfillments = (await results.ReadAsync<StallFulfillment>()).AsList();
        var byId = fulfillments.ToDictionary(fulfillment => fulfillment.Id);
        foreach (var fulfillment in fulfillments)
        {
            fulfillment.Order = order;
            order.StallFulfillments.Add(fulfillment);
        }

        foreach (var item in await results.ReadAsync<OrderItem>())
        {
            if (byId.TryGetValue(item.StallFulfillmentId, out var fulfillment))
            {
                item.StallFulfillment = fulfillment;
                fulfillment.OrderItems.Add(item);
            }
        }

        return order;
    }
}
