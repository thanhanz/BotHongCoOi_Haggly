using Haggly.Application.Abstractions.Catalog;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Catalog;

public sealed class EfProductStallCommandRepository(HagglyDbContext db) : IProductStallCommandRepository
{
    public Task<Stall?> FindActiveStallAsync(Guid id, CancellationToken ct) => db.Stalls.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Product?> FindActiveProductAsync(Guid id, CancellationToken ct) => db.Products.SingleOrDefaultAsync(x => x.Id == id && x.Status == CatalogStatus.ACTIVE, ct);
    public Task<bool> ExistsAsync(Guid stallId, Guid productId, CancellationToken ct) => db.ProductStalls.AnyAsync(x => x.StallId == stallId && x.ProductId == productId, ct);
    public Task<ProductStall?> FindActiveAsync(Guid id, CancellationToken ct) => db.ProductStalls.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(ProductStall value, CancellationToken ct) { db.ProductStalls.Add(value); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
