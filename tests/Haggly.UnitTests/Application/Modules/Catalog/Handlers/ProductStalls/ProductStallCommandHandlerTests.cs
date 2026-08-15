using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.ProductStalls;

public sealed class ProductStallCommandHandlerTests
{
    [Fact]
    public async Task HandleCreate_WhenActorDoesNotOwnStall_ThrowsForbiddenException()
    {
        var stall = new Stall { VendorId = Guid.NewGuid() };
        var repository = new FakeProductStallCommandRepository { Stall = stall };
        var handler = new CreateProductStallHandler(repository);

        await Assert.ThrowsAsync<ProductStallForbiddenException>(() => handler.Handle(
            new CreateProductStallCommand(stall.Id, Guid.NewGuid(), Guid.NewGuid(), null,
                ProductUnit.KG, 1, 10, false), CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenActorOwnsStall_CreatesAssociation()
    {
        var owner = Guid.NewGuid();
        var stall = new Stall { VendorId = owner };
        var product = new Product { Name = "Tomato" };
        var repository = new FakeProductStallCommandRepository { Stall = stall, Product = product };
        var handler = new CreateProductStallHandler(repository);

        var result = await handler.Handle(new CreateProductStallCommand(stall.Id, product.Id, owner,
            "Fresh Tomato", ProductUnit.KG, 0.5m, 45m, true), CancellationToken.None);

        Assert.Equal(stall.Id, result.StallId);
        Assert.Equal(product.Id, result.ProductId);
        Assert.Single(repository.Added);
    }

    private sealed class FakeProductStallCommandRepository : IProductStallCommandRepository
    {
        public Stall? Stall { get; init; }
        public Product? Product { get; init; }
        public List<ProductStall> Added { get; } = [];
        public Task<Stall?> FindActiveStallAsync(Guid id, CancellationToken _) => Task.FromResult(Stall);
        public Task<Product?> FindActiveProductAsync(Guid id, CancellationToken _) => Task.FromResult(Product);
        public Task<bool> ExistsAsync(Guid stallId, Guid productId, CancellationToken _) => Task.FromResult(false);
        public Task<ProductStall?> FindActiveAsync(Guid id, CancellationToken _) => Task.FromResult<ProductStall?>(null);
        public Task AddAsync(ProductStall value, CancellationToken _) { Added.Add(value); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask;
    }
}
