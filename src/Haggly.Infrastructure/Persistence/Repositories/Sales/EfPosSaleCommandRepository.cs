using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Sales;

public sealed class EfPosSaleCommandRepository(HagglyDbContext dbContext)
    : IPosSaleCommandRepository
{
    public Task<PosSale?> FindByClientRequestIdAsync(
        Guid stallId,
        string clientRequestId,
        CancellationToken cancellationToken)
        => dbContext.PosSales
            .Include(sale => sale.Items)
            .SingleOrDefaultAsync(
                sale => sale.StallId == stallId
                    && sale.ClientRequestId == clientRequestId,
                cancellationToken);

    public Task AddAsync(PosSale sale, CancellationToken cancellationToken)
    {
        dbContext.PosSales.Add(sale);
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
            throw new PosSaleConflictException(
                "The inventory record was changed by another request. Refresh and retry.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new PosSaleConflictException(
                "The POS sale already exists or conflicts with another request.");
        }
    }
}
