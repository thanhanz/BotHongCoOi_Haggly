using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Sales;

public sealed class EfOrderCommandRepository(HagglyDbContext dbContext)
    : IOrderCommandRepository
{
    public Task<Order?> FindByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => dbContext.Orders
            .Include(order => order.StallFulfillments)
            .ThenInclude(fulfillment => fulfillment.OrderItems)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public Task<Order?> FindForPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => dbContext.Orders.SingleOrDefaultAsync(
            order => order.Id == orderId,
            cancellationToken);

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Add(order);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new OrderConflictException(
                "The order was changed by another request. Refresh and retry.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new OrderConflictException(
                "The order conflicts with an existing order record.");
        }
    }
}
