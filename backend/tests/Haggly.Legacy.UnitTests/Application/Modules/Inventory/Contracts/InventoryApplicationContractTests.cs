using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Domain.Modules.Inventory;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.Contracts;

public sealed class InventoryApplicationContractTests
{
    [Fact]
    public void InventoryItemDto_FromDomain_ExposesContinuousQuantities()
    {
        var inventory = DomainInventory.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var item = inventory.AddItem(Guid.NewGuid(), 25.5m, Guid.NewGuid(), DateTimeOffset.UtcNow);
        item.Reserve(5m, DateTimeOffset.UtcNow);
        var result = InventoryItemDto.From(item);
        Assert.Equal(25.5m, result.CurrentQuantity);
        Assert.Equal(5m, result.ReservedQuantity);
        Assert.Equal(20.5m, result.AvailableQuantity);
    }

    [Fact]
    public void AdjustValidation_ZeroDelta_ThrowsValidationException()
    {
        var command = new AdjustInventoryCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, "Count", 0);
        Assert.Throws<InventoryValidationException>(() => InventoryValidation.Validate(command));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void LedgerQueryValidation_InvalidPaging_ThrowsValidationException(int page, int pageSize)
    {
        var query = new GetInventoryLedgerQuery(
            Guid.NewGuid(), Guid.NewGuid(), null, null, page, pageSize);
        Assert.Throws<InventoryValidationException>(() => InventoryValidation.Validate(query));
    }
}
