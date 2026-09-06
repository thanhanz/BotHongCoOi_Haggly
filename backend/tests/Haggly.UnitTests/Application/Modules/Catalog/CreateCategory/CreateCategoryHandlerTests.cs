using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.CreateCategory;

public sealed class CreateCategoryHandlerTests
{
    private readonly ICategoryCommandRepository _repository = Substitute.For<ICategoryCommandRepository>();

    [Fact]
    public async Task Handle_ValidCategory_NormalizesSlugAndSaves()
    {
        // Arrange
        _repository.SlugExistsAsync("fresh-produce", null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await new CreateCategoryHandler(_repository).Handle(
            new CreateCategoryCommand(" Fresh Produce ", " Fresh-Produce ", null, null, null, 0), CancellationToken.None);

        // Assert
        Assert.Equal("Fresh Produce", result.Name);
        Assert.Equal("fresh-produce", result.Slug);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsConflictWithoutSaving()
    {
        // Arrange
        _repository.SlugExistsAsync("fresh-produce", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var action = () => new CreateCategoryHandler(_repository).Handle(
            new CreateCategoryCommand("Fresh Produce", "fresh-produce", null, null, null, 0), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryConflictException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
