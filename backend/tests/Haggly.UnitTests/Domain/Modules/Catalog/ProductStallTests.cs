using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Catalog;

public sealed class ProductStallTests
{
    [Fact]
    public void Create_ValidConfiguration_CreatesActiveListing()
    {
        // Arrange
        var stallId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var productId = Guid.Parse("60000000-0000-0000-0000-000000000002");

        // Act
        var productStall = ProductStall.Create(
            stallId, productId, "  Tomato  ", ProductUnit.KG, 1m, 45_000m, true);

        // Assert
        Assert.Equal(stallId, productStall.StallId);
        Assert.Equal(productId, productStall.ProductId);
        Assert.Equal("Tomato", productStall.DisplayName);
        Assert.True(productStall.IsActive);
    }

    [Fact]
    public void UpdateConfiguration_ChangedPrice_UpdatesListingAndVersion()
    {
        // Arrange
        var productStall = ProductStall.Create(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "Tomato", ProductUnit.KG, 1m, 45_000m, true);

        // Act
        productStall.UpdateConfiguration(null, null, null, 50_000m, null, null);

        // Assert
        Assert.Equal(50_000m, productStall.CurrentUnitPrice);
        Assert.Equal(1, productStall.Version);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    public void Create_InvalidQuantityOrPrice_RejectsListing(decimal minimumQuantity, decimal price)
    {
        // Arrange

        // Act
        var action = () => ProductStall.Create(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "Tomato", ProductUnit.KG, minimumQuantity, price, true);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void UpdateConfiguration_InvalidPrice_RejectsWithoutMutation()
    {
        // Arrange
        var productStall = ProductStall.Create(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "Tomato", ProductUnit.KG, 1m, 45_000m, true);

        // Act
        var action = () => productStall.UpdateConfiguration(null, null, null, -1m, null, null);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(45_000m, productStall.CurrentUnitPrice);
        Assert.Equal(0, productStall.Version);
    }

    [Fact]
    public void UpdateConfiguration_UnchangedValues_DoesNotIncrementVersion()
    {
        // Arrange
        var productStall = ProductStall.Create(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "Tomato", ProductUnit.KG, 1m, 45_000m, true);

        // Act
        productStall.UpdateConfiguration("Tomato", ProductUnit.KG, 1m, 45_000m, true, true);

        // Assert
        Assert.Equal(0, productStall.Version);
    }
}
