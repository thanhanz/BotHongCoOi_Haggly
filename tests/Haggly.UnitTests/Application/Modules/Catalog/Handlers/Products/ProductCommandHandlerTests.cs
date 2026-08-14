using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Handlers.Products;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Products;

public sealed class ProductCommandHandlerTests
{
    [Fact]
    public async Task HandleCreate_WhenCommandIsValid_CreatesActiveProduct()
    {
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        var repository = new FakeProductCommandRepository { Categories = [category] };
        var handler = new CreateProductHandler(repository);

        var result = await handler.Handle(
            new CreateProductCommand(category.Id, "  Apple  ", " Fresh apple ", ProductUnit.KG, null),
            CancellationToken.None);

        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("Apple", result.Name);
        Assert.Equal("Fresh apple", result.Description);
        Assert.Equal(ProductUnit.KG, result.DefaultUnit);
        Assert.Equal(CatalogStatus.ACTIVE, result.Status);
        Assert.Single(repository.Products);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task HandleCreate_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new CreateProductHandler(new FakeProductCommandRepository());

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            handler.Handle(
                new CreateProductCommand(Guid.NewGuid(), "Apple", null, ProductUnit.KG, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenCategoryIsInactive_ThrowsNotFoundException()
    {
        var category = new Category
        {
            Name = "Fruit",
            Slug = "fruit",
            Status = CatalogStatus.INACTIVE
        };
        var handler = new CreateProductHandler(
            new FakeProductCommandRepository { Categories = [category] });

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            handler.Handle(
                new CreateProductCommand(category.Id, "Apple", null, ProductUnit.KG, null),
                CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleCreate_WhenNameIsBlank_ThrowsValidationException(string name)
    {
        var handler = new CreateProductHandler(new FakeProductCommandRepository());

        await Assert.ThrowsAsync<ProductValidationException>(() =>
            handler.Handle(
                new CreateProductCommand(Guid.NewGuid(), name, null, ProductUnit.KG, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenNameExceedsMaximumLength_ThrowsValidationException()
    {
        var handler = new CreateProductHandler(new FakeProductCommandRepository());

        await Assert.ThrowsAsync<ProductValidationException>(() =>
            handler.Handle(
                new CreateProductCommand(Guid.NewGuid(), new string('p', 201), null, ProductUnit.KG, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenDefaultUnitIsInvalid_ThrowsValidationException()
    {
        var handler = new CreateProductHandler(new FakeProductCommandRepository());

        await Assert.ThrowsAsync<ProductValidationException>(() =>
            handler.Handle(
                new CreateProductCommand(Guid.NewGuid(), "Apple", null, (ProductUnit)99, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenNameAlreadyExistsWithinCategory_ThrowsConflictException()
    {
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        var repository = new FakeProductCommandRepository
        {
            Categories = [category],
            Products = [new Product { CategoryId = category.Id, Name = "Apple" }]
        };
        var handler = new CreateProductHandler(repository);

        await Assert.ThrowsAsync<ProductConflictException>(() =>
            handler.Handle(
                new CreateProductCommand(category.Id, "Apple", null, ProductUnit.KG, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenNameExistsInAnotherCategory_CreatesProduct()
    {
        var fruit = new Category { Name = "Fruit", Slug = "fruit" };
        var vegetables = new Category { Name = "Vegetables", Slug = "vegetables" };
        var repository = new FakeProductCommandRepository
        {
            Categories = [fruit, vegetables],
            Products = [new Product { CategoryId = fruit.Id, Name = "Apple" }]
        };
        var handler = new CreateProductHandler(repository);

        var result = await handler.Handle(
            new CreateProductCommand(vegetables.Id, "Apple", null, ProductUnit.PIECE, null),
            CancellationToken.None);

        Assert.Equal(vegetables.Id, result.CategoryId);
        Assert.Equal(2, repository.Products.Count);
    }

    private sealed class FakeProductCommandRepository : IProductCommandRepository
    {
        public List<Category> Categories { get; set; } = [];
        public List<Product> Products { get; set; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task<bool> NameExistsAsync(
            Guid categoryId,
            string name,
            Guid? excludingId,
            CancellationToken cancellationToken)
            => Task.FromResult(Products.Any(product =>
                product.DeletedAt is null
                && product.CategoryId == categoryId
                && product.Name == name
                && (excludingId is null || product.Id != excludingId)));

        public Task<Category?> FindActiveCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken)
            => Task.FromResult(Categories.SingleOrDefault(category =>
                category.Id == categoryId
                && category.DeletedAt is null
                && category.Status == CatalogStatus.ACTIVE));

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            Products.Add(product);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
