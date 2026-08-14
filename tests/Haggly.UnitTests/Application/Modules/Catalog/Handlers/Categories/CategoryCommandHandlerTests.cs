using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Handlers.Categories;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Categories;

public sealed class CategoryCommandHandlerTests
{
    [Fact]
    public async Task HandleCreate_WhenRootCategoryCommandIsValid_CreatesActiveCategory()
    {
        var repository = new FakeCategoryCommandRepository();
        var handler = new CreateCategoryHandler(repository);

        var result = await handler.Handle(
            new CreateCategoryCommand(
                "Fresh Vegetables",
                "fresh-vegetables",
                "Fresh produce.",
                null,
                null,
                1),
            CancellationToken.None);

        Assert.Equal("Fresh Vegetables", result.Name);
        Assert.Equal("fresh-vegetables", result.Slug);
        Assert.Equal(CatalogStatus.ACTIVE, result.Status);
        var category = Assert.Single(repository.Categories);
        Assert.Null(category.ParentCategoryId);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task HandleCreate_WhenParentCategoryExists_CreatesChildCategory()
    {
        var parent = new Category { Name = "Fresh Food", Slug = "fresh-food" };
        var repository = new FakeCategoryCommandRepository { Categories = [parent] };
        var handler = new CreateCategoryHandler(repository);

        var result = await handler.Handle(
            new CreateCategoryCommand("Leafy Greens", "leafy-greens", null, null, parent.Id, 0),
            CancellationToken.None);

        Assert.Equal(parent.Id, result.ParentCategoryId);
        Assert.Equal(parent.Id, repository.Categories.Last().ParentCategoryId);
    }

    [Theory]
    [InlineData("", "fresh-vegetables")]
    [InlineData("Fresh Vegetables", "")]
    [InlineData("   ", "fresh-vegetables")]
    [InlineData("Fresh Vegetables", "   ")]
    public async Task HandleCreate_WhenNameOrSlugIsBlank_ThrowsValidationException(
        string name,
        string slug)
    {
        var handler = new CreateCategoryHandler(new FakeCategoryCommandRepository());

        await Assert.ThrowsAsync<CategoryValidationException>(() =>
            handler.Handle(new CreateCategoryCommand(name, slug, null, null, null, 0), CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenNameOrSlugExceedsMaximumLength_ThrowsValidationException()
    {
        var handler = new CreateCategoryHandler(new FakeCategoryCommandRepository());

        await Assert.ThrowsAsync<CategoryValidationException>(() =>
            handler.Handle(
                new CreateCategoryCommand(new string('n', 201), new string('s', 201), null, null, null, 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenDisplayOrderIsNegative_ThrowsValidationException()
    {
        var handler = new CreateCategoryHandler(new FakeCategoryCommandRepository());

        await Assert.ThrowsAsync<CategoryValidationException>(() =>
            handler.Handle(
                new CreateCategoryCommand("Fresh Vegetables", "fresh-vegetables", null, null, null, -1),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenParentCategoryDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new CreateCategoryHandler(new FakeCategoryCommandRepository());

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            handler.Handle(
                new CreateCategoryCommand(
                    "Leafy Greens",
                    "leafy-greens",
                    null,
                    null,
                    Guid.NewGuid(),
                    0),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenSlugAlreadyExists_ThrowsConflictException()
    {
        var repository = new FakeCategoryCommandRepository
        {
            Categories = [new Category { Name = "Fresh Vegetables", Slug = "fresh-vegetables" }]
        };
        var handler = new CreateCategoryHandler(repository);

        await Assert.ThrowsAsync<CategoryConflictException>(() =>
            handler.Handle(
                new CreateCategoryCommand("Other Vegetables", "fresh-vegetables", null, null, null, 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenValuesContainWhitespaceAndUppercaseSlug_TrimsAndNormalizesValues()
    {
        var repository = new FakeCategoryCommandRepository();
        var handler = new CreateCategoryHandler(repository);

        var result = await handler.Handle(
            new CreateCategoryCommand("  Fresh Vegetables  ", "  Fresh-Vegetables  ", null, null, null, 0),
            CancellationToken.None);

        Assert.Equal("Fresh Vegetables", result.Name);
        Assert.Equal("fresh-vegetables", result.Slug);
        Assert.Equal("Fresh Vegetables", repository.Categories.Single().Name);
        Assert.Equal("fresh-vegetables", repository.Categories.Single().Slug);
    }

    private sealed class FakeCategoryCommandRepository : ICategoryCommandRepository
    {
        public List<Category> Categories { get; set; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludingId,
            CancellationToken cancellationToken)
            => Task.FromResult(Categories.Any(category =>
                category.DeletedAt is null
                && category.Slug == slug
                && (excludingId is null || category.Id != excludingId)));

        public Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Categories.SingleOrDefault(category =>
                category.Id == id && category.DeletedAt is null));

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
