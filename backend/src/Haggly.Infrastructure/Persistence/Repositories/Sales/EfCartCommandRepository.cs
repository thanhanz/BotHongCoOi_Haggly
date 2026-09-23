using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Sales;

public sealed class EfCartCommandRepository(HagglyDbContext dbContext,
  ILogger<EfCartCommandRepository> logger
)
    : ICartCommandRepository
{
    public Task<Cart?> FindByBuyerIdAsync(
        Guid buyerId,
        CancellationToken cancellationToken)
        => dbContext.Carts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.BuyerId == buyerId, cancellationToken);

    public Task AddAsync(Cart cart, CancellationToken cancellationToken)
    {
        dbContext.Carts.Add(cart);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
          logger.LogError(
            exception,
            "Cart concurrency failure. Entries: {Entries}",
            exception.Entries.Select(entry => new
            {
              Entity = entry.Metadata.ClrType.Name,
              State = entry.State.ToString()
            }));
          
            throw new CartConflictException(
                "The cart was changed by another request. Refresh and retry.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new CartConflictException(
                "The cart conflicts with an existing cart item or buyer cart.");
        }
    }
}
