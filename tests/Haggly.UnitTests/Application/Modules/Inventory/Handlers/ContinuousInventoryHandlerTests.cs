using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.Handlers;

public sealed class ContinuousInventoryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_AddInventoryItem_AddsItemToOwnedInventory()
    {
        var ownerId = Guid.NewGuid();
        var stall = new Stall { VendorId = ownerId, Status = StallStatus.ACTIVE };
        var inventory = DomainInventory.Create(stall.Id, ownerId, Now);
        var product = new Product { Name = "Tomato", Status = CatalogStatus.ACTIVE };
        var productStall = ProductStall.Create(stall.Id, product.Id, null, ProductUnit.KG, 1m, 40_000m, false);
        productStall.Product = product;
        var repository = new FakeRepository(inventory);
        var handler = new AddInventoryItemHandler(repository,
            new FakeReferences(stall, productStall), new InlineUnitOfWork(), new FixedClock());

        var result = await handler.Handle(
            new AddInventoryItemCommand(stall.Id, ownerId, productStall.Id, 12m), CancellationToken.None);

        Assert.Equal(12m, result.CurrentQuantity);
        Assert.Single(inventory.Items);
        Assert.True(repository.Saved);
    }

    [Fact]
    public async Task Handle_AdjustWithStaleVersion_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = new Stall { VendorId = ownerId, Status = StallStatus.ACTIVE };
        var inventory = DomainInventory.Create(stall.Id, ownerId, Now);
        var item = inventory.AddItem(Guid.NewGuid(), 5m, ownerId, Now);
        item.UpdateReservedQuantity(1m);
        var handler = new AdjustInventoryHandler(new FakeRepository(inventory),
            new FakeReferences(stall, null), new InlineUnitOfWork(), new FixedClock());

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new AdjustInventoryCommand(stall.Id, item.Id, ownerId, 1m, "Delivery", 0), CancellationToken.None));
    }

    private sealed class FakeRepository(DomainInventory inventory) : IInventoryCommandRepository
    {
        public bool Saved { get; private set; }
        public Task<DomainInventory?> FindInventoryAsync(Guid stallId, CancellationToken ct) => Task.FromResult<DomainInventory?>(inventory);
        public Task<InventoryItem?> FindItemAsync(Guid stallId, Guid itemId, CancellationToken ct)
            => Task.FromResult(inventory.Items.SingleOrDefault(item => item.Id == itemId));
        public Task<bool> ItemExistsAsync(Guid inventoryId, Guid productStallId, CancellationToken ct)
            => Task.FromResult(inventory.Items.Any(item => item.ProductStallId == productStallId));
        public Task AddInventoryAsync(DomainInventory value, CancellationToken ct) => Task.CompletedTask;
        public Task AddItemAsync(InventoryItem item, CancellationToken ct) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken ct) { Saved = true; return Task.CompletedTask; }
    }

    private sealed class FakeReferences(Stall stall, ProductStall? productStall) : IInventoryReferenceQuery
    {
        public Task<Stall?> FindActiveStallAsync(Guid id, CancellationToken ct) => Task.FromResult<Stall?>(stall);
        public Task<ProductStall?> FindActiveProductStallAsync(Guid stallId, Guid id, CancellationToken ct)
            => Task.FromResult(productStall?.Id == id ? productStall : null);
    }

    private sealed class InlineUnitOfWork : IInventoryUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
            => operation(ct);
    }

    private sealed class FixedClock : IBusinessClock
    {
        public DateTimeOffset GetNow() => Now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(Now.Date);
    }
}
