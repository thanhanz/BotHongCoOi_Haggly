using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory.Contracts;

public sealed class InventoryApplicationContractTests
{
    [Fact]
    public void BusinessClock_WhenUtcTimeIsBeforeLocalMidnight_UsesConfiguredBusinessDate()
    {
        var utcNow = new DateTimeOffset(2026, 8, 14, 17, 30, 0, TimeSpan.Zero);
        var timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "MvpMarket",
            TimeSpan.FromHours(7),
            "MVP market",
            "MVP market");
        var clock = new BusinessClock(new FixedTimeProvider(utcNow), timeZone);

        Assert.Equal(utcNow, clock.GetNow());
        Assert.Equal(new DateOnly(2026, 8, 15), clock.GetBusinessDate());
    }

    [Fact]
    public void ListingDto_FromDomain_ExposesInventorySnapshotAndQuantities()
    {
        var listing = DailyProductListing.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            publicUnitPrice: 45_000m,
            openingQuantity: 25.5m,
            actorId: Guid.NewGuid(),
            occurredAt: DateTimeOffset.UtcNow);

        var result = DailyProductListingDto.From(listing);

        Assert.Equal(listing.Id, result.Id);
        Assert.Equal(listing.ProductStallId, result.ProductStallId);
        Assert.Equal("Tomato", result.ProductNameSnapshot);
        Assert.Equal(25.5m, result.OpeningQuantity);
        Assert.Equal(25.5m, result.CurrentQuantity);
        Assert.Equal(25.5m, result.AvailableQuantity);
        Assert.Equal(0L, result.Version);
    }

    [Fact]
    public void OpenSessionValidation_WhenProductStallIsDuplicated_ThrowsValidationException()
    {
        var productStallId = Guid.NewGuid();
        var command = new OpenInventorySessionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Notes: "Morning count",
            Listings:
            [
                new InventoryListingInput(productStallId, 2m, null),
                new InventoryListingInput(productStallId, 3m, 100m)
            ]);

        Assert.Throws<InventoryValidationException>(() => InventoryValidation.Validate(command));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void LedgerQueryValidation_WhenPagingIsInvalid_ThrowsValidationException(int page, int pageSize)
    {
        var query = new GetInventoryLedgerQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessDate: null,
            ListingId: null,
            TransactionType: null,
            page,
            pageSize);

        Assert.Throws<InventoryValidationException>(() => InventoryValidation.Validate(query));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
