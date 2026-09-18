using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery;
using Haggly.Application.Modules.Discovery.Commands;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;
using Haggly.Domain.Modules.Discovery;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Discovery;

public sealed class DiscoveryHandlerTests
{
    [Fact]
    public async Task Search_AccentInsensitiveExactHit_ReturnsCandidate()
    {
        // Arrange
        var dishId = Guid.NewGuid();
        var query = new FakeDiscoveryQuery { Dish = new(dishId, "dish3428", "Bún bò Huế", "mon nuoc") };
        var handler = new SearchCommonDishesHandler(query);

        // Act
        var result = await handler.Handle(new SearchCommonDishesQuery("bun bo hue"), CancellationToken.None);

        // Assert
        Assert.Single(result.Candidates);
        Assert.Equal("bun bo hue", query.LastNormalizedQuery);
    }

    [Fact]
    public async Task Proposal_EligibleListings_SelectsCheapestAndReturnsAlternative()
    {
        // Arrange
        var dishId = Guid.NewGuid(); var ingredientId = Guid.NewGuid();
        var cheaperProduct = Guid.NewGuid(); var expensiveProduct = Guid.NewGuid();
        var query = new FakeDiscoveryQuery
        {
            Rows =
            [
                Row(dishId, ingredientId, expensiveProduct, "Nước mắm ngon", 40_000m),
                Row(dishId, ingredientId, cheaperProduct, "Nước mắm ngon", 20_000m)
            ]
        };
        var handler = new GetDishProposalHandler(query);

        // Act
        var result = await handler.Handle(new GetDishProposalQuery(dishId), CancellationToken.None);

        // Assert
        var ingredient = Assert.Single(result.Ingredients);
        Assert.Equal("AVAILABLE", ingredient.Status);
        Assert.Equal(cheaperProduct, ingredient.SelectedListing!.ProductId);
        Assert.Single(ingredient.Alternatives);
    }

    [Fact]
    public async Task Proposal_BelowMinimumOrderQuantity_ReturnsOutOfStock()
    {
        // Arrange
        var dishId = Guid.NewGuid();
        var row = Row(dishId, Guid.NewGuid(), Guid.NewGuid(), "Nước mắm", 20_000m) with { CurrentQuantity = 2m, ReservedQuantity = 1m, MinimumOrderQuantity = 2m };
        var handler = new GetDishProposalHandler(new FakeDiscoveryQuery { Rows = [row] });

        // Act
        var result = await handler.Handle(new GetDishProposalQuery(dishId), CancellationToken.None);

        // Assert
        Assert.Equal("OUT_OF_STOCK", Assert.Single(result.Ingredients).Status);
    }

    private static ProposalSourceRow Row(Guid dishId, Guid ingredientId, Guid productId, string productName, decimal price) => new(dishId, "Bún bò Huế", ingredientId, "CI_NUOC_MAM", "Nước mắm", productId, productName, true, Guid.NewGuid(), null, "CHAI", 1m, price, true, Guid.NewGuid(), 10m, 0m, Guid.NewGuid(), "Sạp A", true, true);

    [Fact]
    public async Task SetProductMapping_Approved_UsesCommandRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var repository = new FakeDiscoveryCommandRepository();
        var handler = new SetProductIngredientMappingHandler(repository, TimeProvider.System);

        // Act
        await handler.Handle(
            new SetProductIngredientMappingCommand(productId, ingredientId, "APPROVED"),
            CancellationToken.None);

        // Assert
        Assert.Equal(productId, repository.ProductId);
        Assert.Equal(ingredientId, repository.CanonicalIngredientId);
        Assert.Equal(ProductIngredientMappingStatus.APPROVED, repository.Status);
        Assert.Equal(ProductIngredientMappingMethod.MANUAL, repository.Method);
    }

    private sealed class FakeDiscoveryQuery : IDiscoveryQuery
    {
        public CommonDishResult? Dish { get; init; }
        public IReadOnlyList<ProposalSourceRow> Rows { get; init; } = [];
        public string? LastNormalizedQuery { get; private set; }
        public Task<CommonDishResult?> FindDishAsync(string normalizedName, CancellationToken cancellationToken) { LastNormalizedQuery = normalizedName; return Task.FromResult(Dish); }
        public Task<bool> DishExistsAsync(Guid dishId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<ProposalSourceRow>> GetProposalRowsAsync(Guid dishId, CancellationToken cancellationToken) => Task.FromResult(Rows);
        public Task<IReadOnlyList<CanonicalIngredientResult>> FindCanonicalIngredientsAsync(string normalizedQuery, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CanonicalIngredientResult>>([]);
    }

    private sealed class FakeDiscoveryCommandRepository : IDiscoveryCommandRepository
    {
        public Guid ProductId { get; private set; }
        public Guid? CanonicalIngredientId { get; private set; }
        public ProductIngredientMappingStatus Status { get; private set; }
        public ProductIngredientMappingMethod Method { get; private set; }

        public Task SetProductMappingAsync(
            Guid productId,
            Guid? canonicalIngredientId,
            ProductIngredientMappingStatus status,
            ProductIngredientMappingMethod method,
            DateTimeOffset at,
            CancellationToken cancellationToken)
        {
            ProductId = productId;
            CanonicalIngredientId = canonicalIngredientId;
            Status = status;
            Method = method;
            return Task.CompletedTask;
        }
    }
}
