using Haggly.Application.Abstractions.Markets;
using Haggly.Domain.Modules.Markets;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Markets;

public sealed class EfMarketCommandRepository(HagglyDbContext dbContext)
    : IMarketCommandRepository
{
    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken)
        => dbContext.Markets.AnyAsync(
            market => market.Code == code
                && (excludingId == null || market.Id != excludingId),
            cancellationToken);

    public Task<Market?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Markets.SingleOrDefaultAsync(market => market.Id == id, cancellationToken);

    public Task AddAsync(Market market, CancellationToken cancellationToken)
    {
        dbContext.Markets.Add(market);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
